using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Bookings;

/// <summary>
/// C6 regression: updating a materialized Booking (cancel / reschedule / confirm) threw
/// "The property 'Booking.TotalPrice#Price.BookingId' is part of a key and so cannot be modified" — the owned
/// value objects sharing the booking key were treated as modifiable keys on update. Pinning the owned FK to the
/// owner key as ValueGeneratedNever (and the PK ValueGeneratedNever) fixes it. This test proves a booking update
/// now persists.
/// </summary>
public class BookingUpdatePersistenceTests : Infrastructure.ServiceCatalogIntegrationTestBase
{
    public BookingUpdatePersistenceTests(Infrastructure.ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory) { }

    private async Task<Guid> SeedRequestedBookingAsync()
    {
        var (provider, services) = await CreateProviderWithServicesAsync(1);

        var policy = BookingPolicy.Create(1, 90, 24, 0m, true, 24, requireDeposit: false, depositPercentage: 0m);
        var booking = Booking.CreateBookingRequest(
            provider.OwnerId, provider.Id, services[0].Id, staffId: Guid.NewGuid(),
            startTime: DateTime.UtcNow.AddDays(2), duration: Duration.FromMinutes(60),
            totalPrice: Price.Create(100m, "USD"), policy: policy);

        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IBookingWriteRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>();
        await repo.SaveAsync(booking);              // production add path
        await uow.CommitAsync();                    // dispatches + clears events, keeps Version in sync
        return booking.Id.Value;
    }

    [Fact]
    public async Task Confirming_a_materialized_booking_persists_without_the_owned_key_error()
    {
        var id = await SeedRequestedBookingAsync();

        Exception? caught = null;
        using (var scope = Factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IBookingWriteRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>();
            var booking = await repo.GetByIdAsync(BookingId.From(id));
            booking!.Confirm();
            await repo.UpdateAsync(booking, System.Threading.CancellationToken.None);
            try { await uow.CommitAsync(); }
            catch (Exception ex) { caught = ex; }
        }

        caught.Should().BeNull("a booking update must not fail with the owned-key error");

        using (var scope = Factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IBookingWriteRepository>();
            var after = await repo.GetByIdAsync(BookingId.From(id));
            after!.Status.Should().Be(BookingStatus.Confirmed);
        }
    }

    [Fact]
    public async Task Cancelling_a_materialized_booking_persists()
    {
        var id = await SeedRequestedBookingAsync();

        Exception? caught = null;
        using (var scope = Factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IBookingWriteRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>();
            var booking = await repo.GetByIdAsync(BookingId.From(id));
            booking!.Cancel("customer changed plans");
            await repo.UpdateAsync(booking, System.Threading.CancellationToken.None);
            try { await uow.CommitAsync(); }
            catch (Exception ex) { caught = ex; }
        }

        caught.Should().BeNull();
        using (var scope = Factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IBookingWriteRepository>();
            (await repo.GetByIdAsync(BookingId.From(id)))!.Status.Should().Be(BookingStatus.Cancelled);
        }
    }
}
