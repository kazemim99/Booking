using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.PromotionAggregate;
using AsanRezerve.ServiceCatalog.Domain.Repositories;

namespace AsanRezerve.ServiceCatalog.Application.Promotions
{
    // A salon's own promotions and its participation in platform campaigns. Authorization (admin, owner, or a member
    // who may manage the organization) is the controller's; every handler scopes itself to the route's salon.

    // ---- list ----

    public sealed record GetProviderPromotionsQuery(Guid ProviderId) : IQuery<IReadOnlyList<PromotionDto>>;

    public sealed class GetProviderPromotionsQueryHandler : IQueryHandler<GetProviderPromotionsQuery, IReadOnlyList<PromotionDto>>
    {
        private readonly IPromotionRepository _promotions;
        private readonly IPromotionRedemptionRepository _redemptions;

        public GetProviderPromotionsQueryHandler(IPromotionRepository promotions, IPromotionRedemptionRepository redemptions)
        {
            _promotions = promotions;
            _redemptions = redemptions;
        }

        public async Task<IReadOnlyList<PromotionDto>> Handle(GetProviderPromotionsQuery request, CancellationToken cancellationToken)
        {
            var promotions = await _promotions.ListForProviderAsync(ProviderId.From(request.ProviderId), cancellationToken);
            var usage = await _redemptions.UsageAsync(promotions.Select(p => p.Id).ToList(), cancellationToken);
            var now = DateTime.UtcNow;
            return promotions.Select(p => PromotionDto.From(p, now, usage.GetValueOrDefault(p.Id))).ToList();
        }
    }

    // ---- create ----

    public sealed record CreateProviderPromotionCommand(
        Guid ProviderId, PromotionTermsInput Terms, Guid ActingUserId, Guid? IdempotencyKey = null) : ICommand<PromotionDto>;

    public sealed class CreateProviderPromotionCommandHandler : ICommandHandler<CreateProviderPromotionCommand, PromotionDto>
    {
        private readonly IPromotionRepository _promotions;
        private readonly IServiceReadRepository _services;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;

        public CreateProviderPromotionCommandHandler(
            IPromotionRepository promotions, IServiceReadRepository services, IServiceCatalogUnitOfWork unitOfWork)
        {
            _promotions = promotions;
            _services = services;
            _unitOfWork = unitOfWork;
        }

        public async Task<PromotionDto> Handle(CreateProviderPromotionCommand request, CancellationToken cancellationToken)
        {
            var providerId = ProviderId.From(request.ProviderId);
            var now = DateTime.UtcNow;
            var terms = request.Terms.ToTerms(now);

            await PromotionGuards.EnsureServicesBelongToAsync(_services, providerId, terms.ServiceIds, cancellationToken);
            var promotion = Promotion.CreateForProvider(providerId, terms, request.ActingUserId, now);
            await PromotionGuards.EnsureCodeFreeAsync(_promotions, promotion, cancellationToken);

            await _promotions.AddAsync(promotion, cancellationToken);
            await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);
            return PromotionDto.From(promotion, now);
        }
    }

    // ---- update ----

    public sealed record UpdateProviderPromotionCommand(
        Guid ProviderId, Guid PromotionId, PromotionTermsInput Terms, Guid? IdempotencyKey = null) : ICommand<PromotionDto>;

    public sealed class UpdateProviderPromotionCommandHandler : ICommandHandler<UpdateProviderPromotionCommand, PromotionDto>
    {
        private readonly IPromotionRepository _promotions;
        private readonly IServiceReadRepository _services;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;

        public UpdateProviderPromotionCommandHandler(
            IPromotionRepository promotions, IServiceReadRepository services, IServiceCatalogUnitOfWork unitOfWork)
        {
            _promotions = promotions;
            _services = services;
            _unitOfWork = unitOfWork;
        }

        public async Task<PromotionDto> Handle(UpdateProviderPromotionCommand request, CancellationToken cancellationToken)
        {
            var providerId = ProviderId.From(request.ProviderId);
            var promotion = await PromotionGuards.GetSalonPromotionAsync(_promotions, providerId, request.PromotionId, cancellationToken);
            var now = DateTime.UtcNow;
            var terms = request.Terms.ToTerms(now);

            await PromotionGuards.EnsureServicesBelongToAsync(_services, providerId, terms.ServiceIds, cancellationToken);
            promotion.Update(terms, now);
            await PromotionGuards.EnsureCodeFreeAsync(_promotions, promotion, cancellationToken);

            await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);
            return PromotionDto.From(promotion, now);
        }
    }

    // ---- pause / resume / end ----

    public sealed record ChangeProviderPromotionStatusCommand(
        Guid ProviderId, Guid PromotionId, PromotionLifecycleAction Action, Guid? IdempotencyKey = null) : ICommand<PromotionDto>;

    public sealed class ChangeProviderPromotionStatusCommandHandler : ICommandHandler<ChangeProviderPromotionStatusCommand, PromotionDto>
    {
        private readonly IPromotionRepository _promotions;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;

        public ChangeProviderPromotionStatusCommandHandler(IPromotionRepository promotions, IServiceCatalogUnitOfWork unitOfWork)
        {
            _promotions = promotions;
            _unitOfWork = unitOfWork;
        }

        public async Task<PromotionDto> Handle(ChangeProviderPromotionStatusCommand request, CancellationToken cancellationToken)
        {
            var promotion = await PromotionGuards.GetSalonPromotionAsync(
                _promotions, ProviderId.From(request.ProviderId), request.PromotionId, cancellationToken);
            var now = DateTime.UtcNow;

            PromotionGuards.ApplyLifecycle(promotion, request.Action, byPlatform: false, now);

            await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);
            return PromotionDto.From(promotion, now);
        }
    }

    // ---- platform campaigns, as a salon sees them ----

    public sealed record GetCampaignsForProviderQuery(Guid ProviderId) : IQuery<IReadOnlyList<CampaignForProviderDto>>;

    public sealed class GetCampaignsForProviderQueryHandler : IQueryHandler<GetCampaignsForProviderQuery, IReadOnlyList<CampaignForProviderDto>>
    {
        private readonly IPromotionRepository _promotions;
        private readonly ICampaignEnrollmentRepository _enrollments;

        public GetCampaignsForProviderQueryHandler(IPromotionRepository promotions, ICampaignEnrollmentRepository enrollments)
        {
            _promotions = promotions;
            _enrollments = enrollments;
        }

        public async Task<IReadOnlyList<CampaignForProviderDto>> Handle(GetCampaignsForProviderQuery request, CancellationToken cancellationToken)
        {
            var providerId = ProviderId.From(request.ProviderId);
            var now = DateTime.UtcNow;
            var campaigns = await _promotions.ListJoinableCampaignsAsync(now, cancellationToken);

            var result = new List<CampaignForProviderDto>(campaigns.Count);
            foreach (var campaign in campaigns)
            {
                var enrollment = await _enrollments.GetAsync(campaign.Id, providerId, cancellationToken);
                var joined = enrollment?.IsActive == true;
                result.Add(new CampaignForProviderDto(PromotionDto.From(campaign, now), joined, joined ? enrollment!.JoinedAt : null));
            }

            return result;
        }
    }

    public sealed record JoinCampaignCommand(
        Guid ProviderId, Guid CampaignId, Guid ActingUserId, Guid? IdempotencyKey = null) : ICommand<CampaignForProviderDto>;

    public sealed class JoinCampaignCommandHandler : ICommandHandler<JoinCampaignCommand, CampaignForProviderDto>
    {
        private readonly IPromotionRepository _promotions;
        private readonly ICampaignEnrollmentRepository _enrollments;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;

        public JoinCampaignCommandHandler(
            IPromotionRepository promotions, ICampaignEnrollmentRepository enrollments, IServiceCatalogUnitOfWork unitOfWork)
        {
            _promotions = promotions;
            _enrollments = enrollments;
            _unitOfWork = unitOfWork;
        }

        public async Task<CampaignForProviderDto> Handle(JoinCampaignCommand request, CancellationToken cancellationToken)
        {
            var providerId = ProviderId.From(request.ProviderId);
            var campaign = await _promotions.GetAsync(request.CampaignId, cancellationToken);
            if (campaign is null || campaign.Owner != PromotionOwner.Platform)
                throw new NotFoundException("این کمپین پیدا نشد.");

            var now = DateTime.UtcNow;
            var enrollment = await _enrollments.GetAsync(campaign.Id, providerId, cancellationToken);
            if (enrollment is null)
            {
                enrollment = CampaignEnrollment.Join(campaign, providerId, request.ActingUserId, now);
                await _enrollments.AddAsync(enrollment, cancellationToken);
            }
            else
            {
                enrollment.Rejoin(campaign, request.ActingUserId, now);
            }

            await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);
            return new CampaignForProviderDto(PromotionDto.From(campaign, now), true, enrollment.JoinedAt);
        }
    }

    public sealed record LeaveCampaignCommand(
        Guid ProviderId, Guid CampaignId, Guid ActingUserId, Guid? IdempotencyKey = null) : ICommand<CampaignForProviderDto>;

    public sealed class LeaveCampaignCommandHandler : ICommandHandler<LeaveCampaignCommand, CampaignForProviderDto>
    {
        private readonly IPromotionRepository _promotions;
        private readonly ICampaignEnrollmentRepository _enrollments;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;

        public LeaveCampaignCommandHandler(
            IPromotionRepository promotions, ICampaignEnrollmentRepository enrollments, IServiceCatalogUnitOfWork unitOfWork)
        {
            _promotions = promotions;
            _enrollments = enrollments;
            _unitOfWork = unitOfWork;
        }

        public async Task<CampaignForProviderDto> Handle(LeaveCampaignCommand request, CancellationToken cancellationToken)
        {
            var campaign = await _promotions.GetAsync(request.CampaignId, cancellationToken);
            if (campaign is null || campaign.Owner != PromotionOwner.Platform)
                throw new NotFoundException("این کمپین پیدا نشد.");

            var now = DateTime.UtcNow;
            var enrollment = await _enrollments.GetAsync(campaign.Id, ProviderId.From(request.ProviderId), cancellationToken);
            enrollment?.Leave(now, request.ActingUserId);

            await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);
            return new CampaignForProviderDto(PromotionDto.From(campaign, now), false, null);
        }
    }

    /// <summary>Checks shared by salon and admin handlers.</summary>
    internal static class PromotionGuards
    {
        public static async Task<Promotion> GetSalonPromotionAsync(
            IPromotionRepository promotions, ProviderId providerId, Guid promotionId, CancellationToken cancellationToken)
        {
            var promotion = await promotions.GetAsync(promotionId, cancellationToken);
            // Another salon's promotion is reported as missing, not forbidden: its existence is not this salon's business.
            if (promotion is null || promotion.Owner != PromotionOwner.Provider || promotion.ProviderId != providerId)
                throw new NotFoundException("این تخفیف پیدا نشد.");
            return promotion;
        }

        public static async Task EnsureServicesBelongToAsync(
            IServiceReadRepository services, ProviderId providerId, IReadOnlyCollection<Guid>? serviceIds,
            CancellationToken cancellationToken)
        {
            if (serviceIds is not { Count: > 0 })
                return;

            var own = (await services.GetByProviderIdAsync(providerId, cancellationToken)).Select(s => s.Id.Value).ToHashSet();
            if (serviceIds.Any(id => id != Guid.Empty && !own.Contains(id)))
                throw new DomainValidationException(nameof(PromotionTermsInput.ServiceIds), "خدمت انتخاب‌شده متعلق به این سالن نیست.");
        }

        public static async Task EnsureCodeFreeAsync(
            IPromotionRepository promotions, Promotion promotion, CancellationToken cancellationToken)
        {
            if (promotion.Code is null)
                return;

            if (await promotions.CodeInUseAsync(promotion.Owner, promotion.ProviderId, promotion.Code, promotion.Id, cancellationToken))
                throw new ConflictException($"کد «{promotion.Code}» قبلاً برای تخفیف دیگری استفاده شده است.");
        }

        public static void ApplyLifecycle(Promotion promotion, PromotionLifecycleAction action, bool byPlatform, DateTime nowUtc)
        {
            switch (action)
            {
                case PromotionLifecycleAction.Pause: promotion.Pause(byPlatform, nowUtc); break;
                case PromotionLifecycleAction.Resume: promotion.Resume(byPlatform, nowUtc); break;
                case PromotionLifecycleAction.End: promotion.End(nowUtc); break;
                default: throw new DomainValidationException(nameof(action), "عملیات معتبر نیست.");
            }
        }
    }
}
