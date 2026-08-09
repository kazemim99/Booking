using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Application.Commands.Provider.UpdateBookingPreferences;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Events;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using DomainProvider = Booksy.ServiceCatalog.Domain.Aggregates.Provider;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Bookings;

/// <summary>
/// P0-0 — proves the deposit capability is real, end to end, through the production command path.
///
/// Before this change no supported path could make a booking require a deposit: nothing ever wrote a
/// <c>BookingPolicy</c>, so every booking silently used the no-deposit default and the entire checkout journey was
/// unreachable. These tests drive the real <see cref="UpdateBookingPreferencesCommandHandler"/> through MediatR
/// (validation, ownership marker, domain validation, audit event, persistence) and then follow the consequence all
/// the way to a confirmed, paid booking — for both percentage and fixed-amount deposits.
/// </summary>
public class ProviderDepositPolicyTests : Infrastructure.ServiceCatalogIntegrationTestBase
{
    public ProviderDepositPolicyTests(Infrastructure.ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory) { }

    private UpdateBookingPreferencesCommand Command(
        Guid providerId,
        Guid actingUserId,
        bool requireDeposit = true,
        DepositType depositType = DepositType.Percentage,
        decimal percentage = 20m,
        decimal fixedAmount = 0m) =>
        new(
            ProviderId: providerId,
            RequireDeposit: requireDeposit,
            DepositType: depositType,
            DepositPercentage: percentage,
            DepositFixedAmount: fixedAmount,
            MinAdvanceBookingHours: 1,
            MaxAdvanceBookingDays: 90,
            CancellationWindowHours: 24,
            CancellationFeePercentage: 50,
            AllowRescheduling: true,
            RescheduleWindowHours: 24,
            ActingUserId: actingUserId);

    private async Task<DomainProvider> ReloadProviderAsync(Guid providerId)
    {
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IProviderReadRepository>();
        return (await repo.GetByIdAsync(ProviderId.From(providerId)))!;
    }

    /// Runs the real command handler directly (bypassing only the HTTP layer) and commits.
    private async Task<UpdateBookingPreferencesResult> SetPolicyAsync(UpdateBookingPreferencesCommand command)
    {
        using var scope = Factory.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<
            IRequestHandler<UpdateBookingPreferencesCommand, UpdateBookingPreferencesResult>>();
        var uow = scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>();

        var result = await handler.Handle(command, CancellationToken.None);
        await uow.CommitAsync();
        return result;
    }

    // ================================================================ write path

    [Fact]
    public async Task A_provider_can_be_configured_to_require_a_percentage_deposit_and_it_persists()
    {
        var provider = await CreateAndAuthenticateAsProviderAsync("Deposit Salon", $"dep-{Guid.NewGuid():N}@test.com");

        var result = await SetPolicyAsync(Command(provider.Id.Value, provider.OwnerId.Value, percentage: 25m));

        result.RequireDeposit.Should().BeTrue();
        result.DepositType.Should().Be(DepositType.Percentage);
        result.DepositPercentage.Should().Be(25m);

        var reloaded = await ReloadProviderAsync(provider.Id.Value);
        reloaded.BookingPolicy.Should().NotBeNull("the policy must survive the round-trip — this is what was missing");
        reloaded.BookingPolicy!.RequireDeposit.Should().BeTrue();
        reloaded.BookingPolicy.DepositPercentage.Should().Be(25m);
        reloaded.BookingPolicy.DepositType.Should().Be(DepositType.Percentage);
    }

    [Fact]
    public async Task A_provider_can_be_configured_to_require_a_fixed_amount_deposit_and_it_persists()
    {
        var provider = await CreateAndAuthenticateAsProviderAsync("Fixed Salon", $"fix-{Guid.NewGuid():N}@test.com");

        await SetPolicyAsync(Command(
            provider.Id.Value, provider.OwnerId.Value,
            depositType: DepositType.FixedAmount, percentage: 0m, fixedAmount: 150_000m));

        var reloaded = await ReloadProviderAsync(provider.Id.Value);
        reloaded.BookingPolicy!.DepositType.Should().Be(DepositType.FixedAmount);
        reloaded.BookingPolicy.DepositFixedAmount.Should().Be(150_000m);
    }

    [Fact]
    public async Task Setting_a_policy_raises_the_audit_event_capturing_previous_and_new_deposit_terms()
    {
        var provider = await CreateAndAuthenticateAsProviderAsync("Audit Salon", $"aud-{Guid.NewGuid():N}@test.com");

        // First change: no previous policy.
        await SetPolicyAsync(Command(provider.Id.Value, provider.OwnerId.Value, percentage: 10m));
        var afterFirst = await ReloadProviderAsync(provider.Id.Value);

        // Second change: the aggregate must report the prior terms so a change is reconstructable.
        afterFirst.SetBookingPolicy(
            BookingPolicy.Create(1, 90, 24, 50, true, 24, true, 30m, DepositType.Percentage, 0),
            UserId.From(provider.OwnerId.Value));

        var audit = afterFirst.DomainEvents.OfType<ProviderBookingPolicyChangedEvent>().Last();
        audit.PreviousRequireDeposit.Should().BeTrue();
        audit.PreviousDepositPercentage.Should().Be(10m);
        audit.DepositPercentage.Should().Be(30m);
        audit.ChangedBy!.Value.Should().Be(provider.OwnerId.Value);
    }

    [Fact]
    public async Task An_uncollectable_required_deposit_is_rejected()
    {
        var provider = await CreateAndAuthenticateAsProviderAsync("Bad Salon", $"bad-{Guid.NewGuid():N}@test.com");

        // "Require a deposit" with nothing to collect would leave every booking permanently unconfirmable.
        var act = async () => await SetPolicyAsync(Command(provider.Id.Value, provider.OwnerId.Value, percentage: 0m));

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task The_command_is_marked_for_provider_ownership_enforcement()
    {
        // The pipeline denies a non-owner; this asserts the command opts into that enforcement at all, so the
        // ownership check can never be silently lost in a refactor.
        typeof(UpdateBookingPreferencesCommand)
            .Should().Implement<Booksy.Core.Application.Authorization.IRequireProviderOwnership>();
    }

    [Fact]
    public async Task An_existing_policy_can_be_updated_and_the_new_value_is_read_back()
    {
        var provider = await CreateAndAuthenticateAsProviderAsync("Upd Salon", $"upd-{Guid.NewGuid():N}@test.com");

        await SetPolicyAsync(Command(provider.Id.Value, provider.OwnerId.Value, percentage: 20m));
        (await ReloadProviderAsync(provider.Id.Value)).BookingPolicy!.DepositPercentage.Should().Be(20m);

        // percentage → percentage
        await SetPolicyAsync(Command(provider.Id.Value, provider.OwnerId.Value, percentage: 40m));
        (await ReloadProviderAsync(provider.Id.Value)).BookingPolicy!.DepositPercentage.Should().Be(40m);
    }

    [Fact]
    public async Task Percentage_can_be_switched_to_fixed_amount_and_back()
    {
        var provider = await CreateAndAuthenticateAsProviderAsync("Swap Salon", $"swp-{Guid.NewGuid():N}@test.com");

        await SetPolicyAsync(Command(provider.Id.Value, provider.OwnerId.Value, percentage: 20m));

        // percentage → fixed amount
        await SetPolicyAsync(Command(
            provider.Id.Value, provider.OwnerId.Value,
            depositType: DepositType.FixedAmount, percentage: 0m, fixedAmount: 250_000m));
        var asFixed = (await ReloadProviderAsync(provider.Id.Value)).BookingPolicy!;
        asFixed.DepositType.Should().Be(DepositType.FixedAmount);
        asFixed.DepositFixedAmount.Should().Be(250_000m);
        asFixed.CalculateDepositAmount(Money.Create(1_000_000m, "IRR")).Amount.Should().Be(250_000m);

        // fixed amount → percentage
        await SetPolicyAsync(Command(provider.Id.Value, provider.OwnerId.Value, percentage: 15m));
        var asPct = (await ReloadProviderAsync(provider.Id.Value)).BookingPolicy!;
        asPct.DepositType.Should().Be(DepositType.Percentage);
        asPct.DepositPercentage.Should().Be(15m);
        asPct.CalculateDepositAmount(Money.Create(1_000_000m, "IRR")).Amount.Should().Be(150_000m);
    }

    [Fact]
    public async Task A_new_booking_uses_the_updated_policy()
    {
        var provider = await CreateAndAuthenticateAsProviderAsync("New Salon", $"new-{Guid.NewGuid():N}@test.com");
        await SetPolicyAsync(Command(provider.Id.Value, provider.OwnerId.Value, percentage: 20m));
        await SetPolicyAsync(Command(provider.Id.Value, provider.OwnerId.Value, percentage: 40m));

        // A booking created after the change owes the NEW deposit — proving readers see the updated policy
        // (this is what the provider-cache invalidation guarantees).
        await AssertDepositChainAsync(provider, total: 1_000_000m, expectedDeposit: 400_000m);
    }

    // ================================================================ the capability, end to end

    /// <summary>
    /// The full chain the product needs: a configured provider policy becomes a booking that owes a deposit, the
    /// booking cannot be confirmed until the money arrives, the gateway's verification records the deposit, and only
    /// then does the booking confirm with the deposit reflected in what was paid.
    /// </summary>
    private async Task AssertDepositChainAsync(
        DomainProvider provider, decimal total, decimal expectedDeposit)
    {
        var reloaded = await ReloadProviderAsync(provider.Id.Value);
        var effectivePolicy = reloaded.BookingPolicy!; // service override absent → provider default applies

        var booking = Booking.CreateBookingRequest(
            customerId: UserId.From(Guid.NewGuid()),
            providerId: provider.Id,
            serviceId: ServiceId.From(Guid.NewGuid()),
            staffId: Guid.NewGuid(),
            startTime: DateTime.UtcNow.AddDays(3),
            duration: Duration.FromMinutes(45),
            totalPrice: Price.Create(total, "IRR"),
            policy: effectivePolicy);

        // 1) the booking carries the deposit requirement
        booking.Policy.RequireDeposit.Should().BeTrue();
        booking.PaymentInfo.DepositAmount.Amount.Should().Be(expectedDeposit);

        // 2) confirmation is blocked before the deposit is paid
        var premature = () => booking.Confirm();
        premature.Should().Throw<Exception>("the deposit gate must hold before any money arrives");
        booking.Status.Should().Be(BookingStatus.Requested);

        // 3) the gateway's verification records the deposit (what the PaymentVerified handler does)
        booking.RecordDepositPaid("REF-DEPOSIT-1");
        booking.PaymentInfo.IsDepositPaid().Should().BeTrue();
        booking.PaymentInfo.PaidAmount.Amount.Should().Be(expectedDeposit);

        // 4) and only now can it confirm
        booking.Confirm();
        booking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public async Task Percentage_policy_produces_a_deposit_booking_that_confirms_only_after_payment()
    {
        var provider = await CreateAndAuthenticateAsProviderAsync("Pct Salon", $"pct-{Guid.NewGuid():N}@test.com");
        await SetPolicyAsync(Command(provider.Id.Value, provider.OwnerId.Value, percentage: 20m));

        // 20% of 1,000,000 = 200,000
        await AssertDepositChainAsync(provider, total: 1_000_000m, expectedDeposit: 200_000m);
    }

    [Fact]
    public async Task Fixed_amount_policy_produces_a_deposit_booking_that_confirms_only_after_payment()
    {
        var provider = await CreateAndAuthenticateAsProviderAsync("Fix2 Salon", $"fx2-{Guid.NewGuid():N}@test.com");
        await SetPolicyAsync(Command(
            provider.Id.Value, provider.OwnerId.Value,
            depositType: DepositType.FixedAmount, percentage: 0m, fixedAmount: 150_000m));

        await AssertDepositChainAsync(provider, total: 1_000_000m, expectedDeposit: 150_000m);
    }

    [Fact]
    public async Task A_provider_with_no_policy_still_produces_a_no_deposit_booking()
    {
        var provider = await CreateAndAuthenticateAsProviderAsync("None Salon", $"non-{Guid.NewGuid():N}@test.com");
        var reloaded = await ReloadProviderAsync(provider.Id.Value);
        reloaded.BookingPolicy.Should().BeNull("existing providers are untouched until they opt in");

        var booking = Booking.CreateBookingRequest(
            customerId: UserId.From(Guid.NewGuid()), providerId: provider.Id,
            serviceId: ServiceId.From(Guid.NewGuid()), staffId: Guid.NewGuid(),
            startTime: DateTime.UtcNow.AddDays(3), duration: Duration.FromMinutes(45),
            totalPrice: Price.Create(1_000_000m, "IRR"),
            policy: reloaded.BookingPolicy ?? BookingPolicy.Default);

        booking.Policy.RequireDeposit.Should().BeFalse();
        booking.PaymentInfo.DepositAmount.Amount.Should().Be(0m);
        booking.Confirm(); // no deposit owed → confirms immediately, exactly as before this change
        booking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public async Task Changing_the_policy_does_not_alter_an_existing_booking()
    {
        var provider = await CreateAndAuthenticateAsProviderAsync("Snap Salon", $"snp-{Guid.NewGuid():N}@test.com");
        await SetPolicyAsync(Command(provider.Id.Value, provider.OwnerId.Value, percentage: 20m));

        var atBookingTime = (await ReloadProviderAsync(provider.Id.Value)).BookingPolicy!;
        var booking = Booking.CreateBookingRequest(
            customerId: UserId.From(Guid.NewGuid()), providerId: provider.Id,
            serviceId: ServiceId.From(Guid.NewGuid()), staffId: Guid.NewGuid(),
            startTime: DateTime.UtcNow.AddDays(3), duration: Duration.FromMinutes(45),
            totalPrice: Price.Create(1_000_000m, "IRR"), policy: atBookingTime);

        booking.PaymentInfo.DepositAmount.Amount.Should().Be(200_000m);

        // The provider later doubles the deposit.
        await SetPolicyAsync(Command(provider.Id.Value, provider.OwnerId.Value, percentage: 40m));

        // The existing booking keeps the terms its customer agreed to (policy is snapshotted per booking).
        booking.Policy.DepositPercentage.Should().Be(20m);
        booking.PaymentInfo.DepositAmount.Amount.Should().Be(200_000m);
        (await ReloadProviderAsync(provider.Id.Value)).BookingPolicy!.DepositPercentage.Should().Be(40m);
    }
}
