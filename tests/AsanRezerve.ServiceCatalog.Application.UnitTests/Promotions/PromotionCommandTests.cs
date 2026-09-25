using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.ServiceCatalog.Application.Abstractions.Persistence;
using AsanRezerve.ServiceCatalog.Application.Promotions;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.PromotionAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using NSubstitute;
using DayOfWeek = AsanRezerve.ServiceCatalog.Domain.Enums.DayOfWeek;

namespace AsanRezerve.ServiceCatalog.Application.UnitTests.Promotions;

/// <summary>
/// Handlers for salon promotions, campaign participation and admin oversight: scoping to the salon, code
/// uniqueness, service ownership, and who may do what to whose promotion.
/// </summary>
public class PromotionCommandTests
{
    private readonly IPromotionRepository _promotions = Substitute.For<IPromotionRepository>();
    private readonly ICampaignEnrollmentRepository _enrollments = Substitute.For<ICampaignEnrollmentRepository>();
    private readonly IServiceReadRepository _services = Substitute.For<IServiceReadRepository>();
    private readonly IServiceCatalogUnitOfWork _unitOfWork = Substitute.For<IServiceCatalogUnitOfWork>();
    private readonly Guid _salon = Guid.NewGuid();
    private readonly Guid _user = Guid.NewGuid();

    private static PromotionTermsInput Input(
        string? activation = "Automatic", string? code = null, string? kind = "Percentage", decimal value = 20,
        IReadOnlyList<Guid>? serviceIds = null, IReadOnlyList<int>? days = null, string? start = null, string? end = null) =>
        new("تخفیف", null, activation, code, kind, value, null, null, false, serviceIds, days, start, end,
            null, null, null, null);

    private static PromotionTerms Terms(PromotionActivation activation = PromotionActivation.Automatic, string? code = null) =>
        new("تخفیف", null, activation, code, DiscountKind.Percentage, 20m, null, null, false, null, null, null, null,
            DateTime.UtcNow.AddDays(-1), null, null, null);

    // ── Input parsing ──

    [Fact]
    public void Enum_names_are_parsed_case_insensitively()
    {
        var terms = Input(activation: "code", code: "ABCD", kind: "fixedamount", value: 5000).ToTerms(DateTime.UtcNow);

        Assert.Equal(PromotionActivation.Code, terms.Activation);
        Assert.Equal(DiscountKind.FixedAmount, terms.DiscountKind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Sometimes")]
    [InlineData("1")]
    public void An_unknown_activation_is_refused(string? activation)
    {
        Assert.Throws<DomainValidationException>(() => Input(activation: activation).ToTerms(DateTime.UtcNow));
    }

    [Fact]
    public void Days_and_times_are_parsed()
    {
        var terms = Input(days: new[] { 6, 0, 6 }, start: "10:00", end: "13:30").ToTerms(DateTime.UtcNow);

        Assert.Equal(new[] { DayOfWeek.Saturday, DayOfWeek.Sunday }, terms.DaysOfWeek!);
        Assert.Equal(new TimeOnly(10, 0), terms.DailyStartTime);
        Assert.Equal(new TimeOnly(13, 30), terms.DailyEndTime);
    }

    [Fact]
    public void Malformed_days_and_times_are_refused()
    {
        Assert.Throws<DomainValidationException>(() => Input(days: new[] { 7 }).ToTerms(DateTime.UtcNow));
        Assert.Throws<DomainValidationException>(() => Input(start: "25:00", end: "26:00").ToTerms(DateTime.UtcNow));
    }

    [Fact]
    public void A_missing_start_means_now()
    {
        var now = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

        Assert.Equal(now, Input().ToTerms(now).StartsAt);
    }

    // ── Salon promotions ──

    [Fact]
    public async Task A_salon_creates_a_promotion()
    {
        var handler = new CreateProviderPromotionCommandHandler(_promotions, _services, _unitOfWork);

        var dto = await handler.Handle(new CreateProviderPromotionCommand(_salon, Input(), _user), default);

        Assert.Equal("Provider", dto.Owner);
        Assert.Equal(_salon, dto.ProviderId);
        Assert.Equal("Active", dto.State);
        await _promotions.Received(1).AddAsync(Arg.Is<Promotion>(p => p.CreatedBy == _user), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveAndPublishEventsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_promotion_cannot_target_another_salons_service()
    {
        _services.GetByProviderIdAsync(ProviderId.From(_salon), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Service>());
        var handler = new CreateProviderPromotionCommandHandler(_promotions, _services, _unitOfWork);

        await Assert.ThrowsAsync<DomainValidationException>(() => handler.Handle(
            new CreateProviderPromotionCommand(_salon, Input(serviceIds: new[] { Guid.NewGuid() }), _user), default));
        await _promotions.DidNotReceive().AddAsync(Arg.Any<Promotion>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_code_the_salon_already_uses_is_a_conflict()
    {
        _promotions.CodeInUseAsync(PromotionOwner.Provider, ProviderId.From(_salon), "SPRING", Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var handler = new CreateProviderPromotionCommandHandler(_promotions, _services, _unitOfWork);

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new CreateProviderPromotionCommand(_salon, Input(activation: "Code", code: "spring"), _user), default));
    }

    [Fact]
    public async Task Another_salons_promotion_reads_as_not_found()
    {
        var foreign = Promotion.CreateForProvider(ProviderId.New(), Terms(), Guid.NewGuid(), DateTime.UtcNow);
        _promotions.GetAsync(foreign.Id, Arg.Any<CancellationToken>()).Returns(foreign);

        var update = new UpdateProviderPromotionCommandHandler(_promotions, _services, _unitOfWork);
        var pause = new ChangeProviderPromotionStatusCommandHandler(_promotions, _unitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            update.Handle(new UpdateProviderPromotionCommand(_salon, foreign.Id, Input()), default));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            pause.Handle(new ChangeProviderPromotionStatusCommand(_salon, foreign.Id, PromotionLifecycleAction.Pause), default));
    }

    [Fact]
    public async Task A_salon_cannot_manage_a_platform_campaign_through_its_own_endpoints()
    {
        var campaign = Promotion.CreatePlatformCampaign(Terms(), Guid.NewGuid(), DateTime.UtcNow);
        _promotions.GetAsync(campaign.Id, Arg.Any<CancellationToken>()).Returns(campaign);
        var pause = new ChangeProviderPromotionStatusCommandHandler(_promotions, _unitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            pause.Handle(new ChangeProviderPromotionStatusCommand(_salon, campaign.Id, PromotionLifecycleAction.Pause), default));
    }

    [Fact]
    public async Task A_salon_pauses_and_ends_its_promotion()
    {
        var own = Promotion.CreateForProvider(ProviderId.From(_salon), Terms(), _user, DateTime.UtcNow);
        _promotions.GetAsync(own.Id, Arg.Any<CancellationToken>()).Returns(own);
        var handler = new ChangeProviderPromotionStatusCommandHandler(_promotions, _unitOfWork);

        var paused = await handler.Handle(new ChangeProviderPromotionStatusCommand(_salon, own.Id, PromotionLifecycleAction.Pause), default);
        Assert.Equal("Paused", paused.State);
        Assert.False(paused.PausedByPlatform);

        var ended = await handler.Handle(new ChangeProviderPromotionStatusCommand(_salon, own.Id, PromotionLifecycleAction.End), default);
        Assert.Equal("Ended", ended.State);
    }

    // ── Campaign participation ──

    [Fact]
    public async Task Joining_creates_the_enrollment_the_first_time_and_reuses_it_after()
    {
        var campaign = Promotion.CreatePlatformCampaign(Terms(), Guid.NewGuid(), DateTime.UtcNow);
        _promotions.GetAsync(campaign.Id, Arg.Any<CancellationToken>()).Returns(campaign);
        var join = new JoinCampaignCommandHandler(_promotions, _enrollments, _unitOfWork);

        var first = await join.Handle(new JoinCampaignCommand(_salon, campaign.Id, _user), default);
        Assert.True(first.IsJoined);
        await _enrollments.Received(1).AddAsync(Arg.Any<CampaignEnrollment>(), Arg.Any<CancellationToken>());

        var existing = CampaignEnrollment.Join(campaign, ProviderId.From(_salon), _user, DateTime.UtcNow);
        existing.Leave(DateTime.UtcNow);
        _enrollments.GetAsync(campaign.Id, ProviderId.From(_salon), Arg.Any<CancellationToken>()).Returns(existing);
        _enrollments.ClearReceivedCalls();

        await join.Handle(new JoinCampaignCommand(_salon, campaign.Id, _user), default);
        Assert.True(existing.IsActive);
        await _enrollments.DidNotReceive().AddAsync(Arg.Any<CampaignEnrollment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_salon_promotion_is_not_a_campaign_to_join()
    {
        var own = Promotion.CreateForProvider(ProviderId.New(), Terms(), _user, DateTime.UtcNow);
        _promotions.GetAsync(own.Id, Arg.Any<CancellationToken>()).Returns(own);
        var join = new JoinCampaignCommandHandler(_promotions, _enrollments, _unitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() => join.Handle(new JoinCampaignCommand(_salon, own.Id, _user), default));
    }

    [Fact]
    public async Task Leaving_deactivates_the_enrollment()
    {
        var campaign = Promotion.CreatePlatformCampaign(Terms(), Guid.NewGuid(), DateTime.UtcNow);
        var enrollment = CampaignEnrollment.Join(campaign, ProviderId.From(_salon), _user, DateTime.UtcNow);
        _promotions.GetAsync(campaign.Id, Arg.Any<CancellationToken>()).Returns(campaign);
        _enrollments.GetAsync(campaign.Id, ProviderId.From(_salon), Arg.Any<CancellationToken>()).Returns(enrollment);

        var result = await new LeaveCampaignCommandHandler(_promotions, _enrollments, _unitOfWork)
            .Handle(new LeaveCampaignCommand(_salon, campaign.Id, _user), default);

        Assert.False(result.IsJoined);
        Assert.False(enrollment.IsActive);
    }

    // ── Admin ──

    [Fact]
    public async Task An_admin_creates_a_platform_campaign()
    {
        var dto = await new CreatePlatformCampaignCommandHandler(_promotions, _unitOfWork)
            .Handle(new CreatePlatformCampaignCommand(Input(), _user), default);

        Assert.Equal("Platform", dto.Owner);
        Assert.Null(dto.ProviderId);
        Assert.Equal(0, dto.JoinedSalons);
    }

    [Fact]
    public async Task An_admin_does_not_rewrite_a_salons_promotion()
    {
        var own = Promotion.CreateForProvider(ProviderId.New(), Terms(), _user, DateTime.UtcNow);
        _promotions.GetAsync(own.Id, Arg.Any<CancellationToken>()).Returns(own);

        await Assert.ThrowsAsync<NotFoundException>(() => new UpdatePlatformCampaignCommandHandler(_promotions, _unitOfWork)
            .Handle(new UpdatePlatformCampaignCommand(own.Id, Input()), default));
    }

    [Fact]
    public async Task An_admin_pause_holds_against_the_salon()
    {
        var own = Promotion.CreateForProvider(ProviderId.From(_salon), Terms(), _user, DateTime.UtcNow);
        _promotions.GetAsync(own.Id, Arg.Any<CancellationToken>()).Returns(own);

        var paused = await new AdminChangePromotionStatusCommandHandler(_promotions, _unitOfWork)
            .Handle(new AdminChangePromotionStatusCommand(own.Id, PromotionLifecycleAction.Pause), default);
        Assert.True(paused.PausedByPlatform);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            new ChangeProviderPromotionStatusCommandHandler(_promotions, _unitOfWork)
                .Handle(new ChangeProviderPromotionStatusCommand(_salon, own.Id, PromotionLifecycleAction.Resume), default));
    }
}
