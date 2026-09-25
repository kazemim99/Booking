using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.PromotionAggregate;
using AsanRezerve.ServiceCatalog.Domain.Repositories;

namespace AsanRezerve.ServiceCatalog.Application.Promotions
{
    // Platform campaigns and oversight of every promotion. Reached only through AdminOnly endpoints.

    public sealed record SearchPromotionsQuery(
        string? Owner, Guid? ProviderId, string? Status, string? Search, int Page = 1, int PageSize = 20)
        : IQuery<PromotionPageDto>;

    public sealed class SearchPromotionsQueryHandler : IQueryHandler<SearchPromotionsQuery, PromotionPageDto>
    {
        private readonly IPromotionRepository _promotions;
        private readonly IPromotionRedemptionRepository _redemptions;
        private readonly ICampaignEnrollmentRepository _enrollments;

        public SearchPromotionsQueryHandler(
            IPromotionRepository promotions, IPromotionRedemptionRepository redemptions, ICampaignEnrollmentRepository enrollments)
        {
            _promotions = promotions;
            _redemptions = redemptions;
            _enrollments = enrollments;
        }

        public async Task<PromotionPageDto> Handle(SearchPromotionsQuery request, CancellationToken cancellationToken)
        {
            var page = Math.Max(1, request.Page);
            var size = Math.Clamp(request.PageSize, 1, 100);
            var result = await _promotions.SearchAsync(new PromotionSearch(
                ParseOptional<PromotionOwner>(request.Owner),
                request.ProviderId is { } id ? ProviderId.From(id) : null,
                ParseOptional<PromotionStatus>(request.Status),
                request.Search,
                page,
                size), cancellationToken);

            var ids = result.Items.Select(p => p.Id).ToList();
            var usage = await _redemptions.UsageAsync(ids, cancellationToken);
            var joined = await _enrollments.ActiveCountsAsync(
                result.Items.Where(p => p.Owner == PromotionOwner.Platform).Select(p => p.Id).ToList(), cancellationToken);
            var names = await _promotions.ProviderNamesAsync(
                result.Items.Where(p => p.ProviderId is not null).Select(p => p.ProviderId!.Value).Distinct().ToList(),
                cancellationToken);

            var now = DateTime.UtcNow;
            var items = result.Items.Select(p => PromotionDto.From(
                    p, now, usage.GetValueOrDefault(p.Id),
                    p.Owner == PromotionOwner.Platform ? joined.GetValueOrDefault(p.Id) : null,
                    p.ProviderId is null ? null : names.GetValueOrDefault(p.ProviderId.Value)))
                .ToList();
            return new PromotionPageDto(items, result.TotalCount, page, size);
        }

        private static TEnum? ParseOptional<TEnum>(string? value) where TEnum : struct, Enum =>
            string.IsNullOrWhiteSpace(value) || value.Equals("all", StringComparison.OrdinalIgnoreCase)
                ? null
                : Enum.TryParse<TEnum>(value, true, out var parsed) && Enum.IsDefined(parsed)
                    ? parsed
                    : throw new DomainValidationException(typeof(TEnum).Name, "فیلتر معتبر نیست.");
    }

    public sealed record GetPromotionDetailsQuery(Guid PromotionId) : IQuery<PromotionDetailsDto>;

    public sealed class GetPromotionDetailsQueryHandler : IQueryHandler<GetPromotionDetailsQuery, PromotionDetailsDto>
    {
        private readonly IPromotionRepository _promotions;
        private readonly IPromotionRedemptionRepository _redemptions;
        private readonly ICampaignEnrollmentRepository _enrollments;

        public GetPromotionDetailsQueryHandler(
            IPromotionRepository promotions, IPromotionRedemptionRepository redemptions, ICampaignEnrollmentRepository enrollments)
        {
            _promotions = promotions;
            _redemptions = redemptions;
            _enrollments = enrollments;
        }

        public async Task<PromotionDetailsDto> Handle(GetPromotionDetailsQuery request, CancellationToken cancellationToken)
        {
            var promotion = await _promotions.GetAsync(request.PromotionId, cancellationToken)
                ?? throw new NotFoundException("این تخفیف پیدا نشد.");

            var usage = await _redemptions.UsageAsync(new[] { promotion.Id }, cancellationToken);
            var participants = promotion.Owner == PromotionOwner.Platform
                ? await _enrollments.ListActiveAsync(promotion.Id, cancellationToken)
                : Array.Empty<CampaignEnrollment>();

            var providerIds = participants.Select(e => e.ProviderId.Value).ToList();
            if (promotion.ProviderId is not null)
                providerIds.Add(promotion.ProviderId.Value);
            var names = await _promotions.ProviderNamesAsync(providerIds.Distinct().ToList(), cancellationToken);

            var dto = PromotionDto.From(
                promotion, DateTime.UtcNow, usage.GetValueOrDefault(promotion.Id),
                promotion.Owner == PromotionOwner.Platform ? participants.Count : null,
                promotion.ProviderId is null ? null : names.GetValueOrDefault(promotion.ProviderId.Value));

            return new PromotionDetailsDto(dto, participants
                .Select(e => new CampaignParticipantDto(e.ProviderId.Value, names.GetValueOrDefault(e.ProviderId.Value), e.JoinedAt))
                .ToList());
        }
    }

    public sealed record CreatePlatformCampaignCommand(
        PromotionTermsInput Terms, Guid ActingUserId, Guid? IdempotencyKey = null) : ICommand<PromotionDto>;

    public sealed class CreatePlatformCampaignCommandHandler : ICommandHandler<CreatePlatformCampaignCommand, PromotionDto>
    {
        private readonly IPromotionRepository _promotions;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;

        public CreatePlatformCampaignCommandHandler(IPromotionRepository promotions, IServiceCatalogUnitOfWork unitOfWork)
        {
            _promotions = promotions;
            _unitOfWork = unitOfWork;
        }

        public async Task<PromotionDto> Handle(CreatePlatformCampaignCommand request, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var campaign = Promotion.CreatePlatformCampaign(request.Terms.ToTerms(now), request.ActingUserId, now);
            await PromotionGuards.EnsureCodeFreeAsync(_promotions, campaign, cancellationToken);

            await _promotions.AddAsync(campaign, cancellationToken);
            await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);
            return PromotionDto.From(campaign, now, joinedSalons: 0);
        }
    }

    public sealed record UpdatePlatformCampaignCommand(
        Guid PromotionId, PromotionTermsInput Terms, Guid? IdempotencyKey = null) : ICommand<PromotionDto>;

    public sealed class UpdatePlatformCampaignCommandHandler : ICommandHandler<UpdatePlatformCampaignCommand, PromotionDto>
    {
        private readonly IPromotionRepository _promotions;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;

        public UpdatePlatformCampaignCommandHandler(IPromotionRepository promotions, IServiceCatalogUnitOfWork unitOfWork)
        {
            _promotions = promotions;
            _unitOfWork = unitOfWork;
        }

        public async Task<PromotionDto> Handle(UpdatePlatformCampaignCommand request, CancellationToken cancellationToken)
        {
            var campaign = await _promotions.GetAsync(request.PromotionId, cancellationToken);
            // A salon's promotion is the salon's to word; the admin may only pause or end it.
            if (campaign is null || campaign.Owner != PromotionOwner.Platform)
                throw new NotFoundException("این کمپین پیدا نشد.");

            var now = DateTime.UtcNow;
            campaign.Update(request.Terms.ToTerms(now), now);
            await PromotionGuards.EnsureCodeFreeAsync(_promotions, campaign, cancellationToken);

            await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);
            return PromotionDto.From(campaign, now);
        }
    }

    /// <summary>Pause, resume or end any promotion — a platform campaign or a salon's own.</summary>
    public sealed record AdminChangePromotionStatusCommand(
        Guid PromotionId, PromotionLifecycleAction Action, Guid? IdempotencyKey = null) : ICommand<PromotionDto>;

    public sealed class AdminChangePromotionStatusCommandHandler : ICommandHandler<AdminChangePromotionStatusCommand, PromotionDto>
    {
        private readonly IPromotionRepository _promotions;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;

        public AdminChangePromotionStatusCommandHandler(IPromotionRepository promotions, IServiceCatalogUnitOfWork unitOfWork)
        {
            _promotions = promotions;
            _unitOfWork = unitOfWork;
        }

        public async Task<PromotionDto> Handle(AdminChangePromotionStatusCommand request, CancellationToken cancellationToken)
        {
            var promotion = await _promotions.GetAsync(request.PromotionId, cancellationToken)
                ?? throw new NotFoundException("این تخفیف پیدا نشد.");
            var now = DateTime.UtcNow;

            PromotionGuards.ApplyLifecycle(promotion, request.Action, byPlatform: true, now);

            await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);
            return PromotionDto.From(promotion, now);
        }
    }
}
