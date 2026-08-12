using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Application.DTOs.Provider;
using Booksy.ServiceCatalog.Application.Queries.Provider.GetRegistrationProgress;
using Booksy.ServiceCatalog.Application.Queries.Provider.SearchProviders;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Enums.Extensions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderById
{
    public sealed class GetProviderByIdQueryHandler : IQueryHandler<GetProviderByIdQuery, ProviderDetailsResult?>
    {
        private readonly IProviderReadRepository _providerRepository;
        private readonly IServiceReadRepository _serviceRepository;
        private readonly ISender _mediator;
        private readonly ILogger<GetProviderByIdQueryHandler> _logger;

        public GetProviderByIdQueryHandler(
            IProviderReadRepository providerRepository,
            IServiceReadRepository serviceRepository,
            ISender mediator,
            ILogger<GetProviderByIdQueryHandler> logger)
        {
            _providerRepository = providerRepository;
            _serviceRepository = serviceRepository;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<ProviderDetailsResult?> Handle(
            GetProviderByIdQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting provider details for ID: {ProviderId}", request.ProviderId);

            var providerId = ProviderId.From(request.ProviderId);
            var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);

            if (provider == null)
            {
                _logger.LogWarning("Provider not found: {ProviderId}", request.ProviderId);
                return null;
            }

            var viewModel = new ProviderDetailsResult
            {
                Id = provider.Id.Value,
                OwnerId = provider.OwnerId.Value,
                BusinessName = provider.Profile.BusinessName,
                Description = provider.Profile.BusinessDescription,
                LogoUrl = provider.Profile.LogoUrl,
                ProfileImageUrl = provider.Profile.ProfileImageUrl,
                Status = provider.Status,
                PrimaryCategory = provider.PrimaryCategory,
                ContactInfo = new DTOs.Provider.ContactInfo(
                    provider.ContactInfo.Email?.Value,
                    provider.ContactInfo.PrimaryPhone?.Value,
                    provider.ContactInfo.SecondaryPhone?.Value,
                    provider.ContactInfo.Website),
                Address = new AddressInfo(
                    provider.Address.Street,
                    provider.Address.City,
                    provider.Address.State,
                               provider.Address.CityId,
                    provider.Address.ProvinceId,
                    provider.Address.PostalCode,

                    provider.Address.Country,
                    provider.Address.Latitude,
                    provider.Address.Longitude),
                BusinessHours = provider.BusinessHours.Select(bh => new BusinessHoursData(
            DayOfWeek: (int)bh.DayOfWeek,
            IsOpen: bh.IsOpen,
            OpenTimeHours: bh.OpenTime?.Hour,
            OpenTimeMinutes: bh.OpenTime?.Minute,
            CloseTimeHours: bh.CloseTime?.Hour,
            CloseTimeMinutes: bh.CloseTime?.Minute,
            Breaks: bh.Breaks.Select(br => new BreakPeriodData(
                StartTimeHours: br.StartTime.Hour,
                StartTimeMinutes: br.StartTime.Minute,
                EndTimeHours: br.EndTime.Hour,
                EndTimeMinutes: br.EndTime.Minute,
                Label: br.Label
            )).ToList()
        )),
                WebsiteUrl = provider.ContactInfo.Website,
                AllowOnlineBooking = provider.AllowOnlineBooking,
                OffersMobileServices = provider.OffersMobileServices,
                IsVerified = provider.VerifiedAt.HasValue,
                AverageRating = provider.AverageRating,
                TotalReviews = 0,
                ServiceCount = provider.Services.Count,
                YearsInBusiness = provider.RegisteredAt.Year > 0
                    ? DateTime.UtcNow.Year - provider.RegisteredAt.Year
                    : 0,
                Tags = provider.Profile.Tags.AsReadOnly(),
                RegisteredAt = provider.RegisteredAt,
                ActivatedAt = provider.ActivatedAt,
                LastActiveAt = provider.LastActiveAt,
                // Hierarchy information
                HierarchyType = provider.HierarchyType,
                IsIndependent = provider.IsIndependent,
                ParentProviderId = provider.ParentProviderId?.Value
            };

            // Fetch parent provider info if this is a linked individual
            if (provider.ParentProviderId != null)
            {
                var parentProvider = await _providerRepository.GetByIdAsync(provider.ParentProviderId, cancellationToken);
                if (parentProvider != null)
                {
                    viewModel.ParentProvider = new ParentProviderInfo
                    {
                        Id = parentProvider.Id.Value,
                        BusinessName = parentProvider.Profile.BusinessName,
                        ProfileImageUrl = parentProvider.Profile.ProfileImageUrl,
                        Status = parentProvider.Status
                    };
                }
            }

            // Fetch staff providers if this is an organization
            if (provider.HierarchyType == Domain.Enums.ProviderHierarchyType.Organization)
            {
                var staffProviders = await _providerRepository.GetStaffByOrganizationIdAsync(providerId, cancellationToken);
                viewModel.StaffProviderCount = staffProviders.Count;
                viewModel.StaffProviders = staffProviders.Select(sp => new StaffProviderInfo
                {
                    Id = sp.Id.Value,
                    BusinessName = sp.Profile.BusinessName,
                    ProfileImageUrl = sp.Profile.ProfileImageUrl,
                    Status = sp.Status,
                    IsIndependent = sp.IsIndependent,
                    AverageRating = sp.AverageRating,
                    ServiceCount = sp.Services.Count
                }).ToList();
            }

            // Bookable staff for the customer-facing booking flow.
            //
            // This was never populated, so `staff` came back [] no matter what IncludeStaff
            // said. The customer app decides whether to show its "choose a team member" step
            // from exactly this list, so a salon with a full roster looked like a one-person
            // shop and every booking silently went to the provider's default resource.
            //
            // Delegated to GetProviderStaffQuery rather than re-projected here: staff live in
            // two models at once (OrganizationMembership + StaffProfile for invited members,
            // and legacy individual sub-Providers awaiting migration) and that handler already
            // merges both, resolves display names through the person directory, and falls back
            // to the salon-provided name for members who have not claimed their account.
            // Duplicating any of that is how these two projections drifted apart to begin
            // with. StaffProviders above stays as it is: it describes only the sub-Provider
            // hierarchy, which is a different question from "who can I book with".
            if (request.IncludeStaff)
            {
                var staffResult = await _mediator.Send(
                    new GetProviderStaff.GetProviderStaffQuery(providerId.Value, IncludeInactive: false),
                    cancellationToken);

                viewModel.Staff = staffResult.Staff
                    // Bookable members only. The management roster legitimately includes
                    // receptionists and owners who just run the business; offering them in the
                    // customer's "choose a team member" step is a dead end, because asking for
                    // their availability correctly returns zero slots.
                    .Where(s => s.ProvidesServices)
                    .Select(s => new ProviderStaffItem
                    {
                        // The membership id — the identifier availability and booking both
                        // expect as StaffId/StaffProviderId.
                        Id = s.Id,
                        FirstName = s.FirstName,
                        LastName = s.LastName,
                        FullName = s.FullName,
                        // StaffDto carries no email: an invited member is identified by phone,
                        // and unclaimed members have no account to take an email from.
                        Email = string.Empty,
                        Phone = s.PhoneNumber,
                        // StaffDto carries the coarse "Owner"/"Staff" label; ProviderStaffItem
                        // wants the finer StaffRole. Anything that is not the owner is a
                        // service provider as far as booking is concerned.
                        Role = string.Equals(s.Role, nameof(StaffRole.Owner), StringComparison.OrdinalIgnoreCase)
                            ? StaffRole.Owner
                            : StaffRole.ServiceProvider,
                        IsActive = s.IsActive,
                        HiredAt = s.HiredAt
                    })
                    .ToList();

                _logger.LogInformation(
                    "Provider {ProviderId} exposes {StaffCount} bookable staff",
                    request.ProviderId,
                    viewModel.Staff.Count);
            }

            if (request.IncludeServices)
            {
                var services = await _serviceRepository.GetByProviderIdAsync(
                    providerId,
                    cancellationToken);

                viewModel.ActiveServicesCount = services.Count;
                viewModel.Services = services.Select(service => new ProviderServiceItem
                {
                    Id = service.Id.Value,
                    Name = service.Name,
                    Description = service.Description,
                    Category = service.Category.ToEnglishName(),
                    BasePrice = service.BasePrice.Amount,
                    Currency = service.BasePrice.Currency,
                    Duration = service.Duration.Value,
                    Status = service.Status
                }).ToList();
            }

            return viewModel;
        }
    }
}