using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Services;
using Booksy.ServiceCatalog.Application.Abstractions;
using Booksy.ServiceCatalog.Domain.Entities;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetRegistrationProgress;

public sealed class GetRegistrationProgressQueryHandler
    : IQueryHandler<GetRegistrationProgressQuery, GetRegistrationProgressResult>
{
    private readonly IProviderWriteRepository _providerRepository;
    private readonly IServiceWriteRepository _serviceRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUrlService _urlService;

    public GetRegistrationProgressQueryHandler(
        IProviderWriteRepository providerRepository,
        IServiceWriteRepository serviceRepository,
        ICurrentUserService currentUserService,
        IUrlService urlService)
    {
        _providerRepository = providerRepository;
        _serviceRepository = serviceRepository;
        _currentUserService = currentUserService;
        _urlService = urlService;
    }

    public async Task<GetRegistrationProgressResult> Handle(
        GetRegistrationProgressQuery request,
        CancellationToken cancellationToken)
    {
        var userId = UserId.From(_currentUserService.UserId ??
            throw new UnauthorizedAccessException("User not authenticated"));

        // First, try to get draft provider (status = Drafted)
        var draftProvider = await _providerRepository
            .GetDraftProviderByOwnerIdAsync(userId, cancellationToken);

        // If no draft found, check if user has a completed/pending provider
        if (draftProvider == null)
        {
            var provider = await _providerRepository
                .GetByOwnerIdAsync(userId, cancellationToken);

            // If provider exists but registration is complete, return completed status
            if (provider != null && provider.IsRegistrationComplete)
            {
                return new GetRegistrationProgressResult(
                    HasDraft: false,
                    CurrentStep: 9, // Registration completed
                    ProviderId: provider.Id.Value,
                    DraftData: null);
            }

            // No provider found at all
            return new GetRegistrationProgressResult(
                HasDraft: false,
                CurrentStep: null,
                ProviderId: null,
                DraftData: null);
        }

        // Map business info
        var businessInfo = new BusinessInfoData(
            BusinessName: draftProvider.Profile.BusinessName,
            BusinessDescription: draftProvider.Profile.BusinessDescription ?? "",
            Category: draftProvider.PrimaryCategory.ToString(),
            PhoneNumber: draftProvider.ContactInfo.PrimaryPhone?.Value ?? "",
            Email: draftProvider.ContactInfo.Email?.Value ?? "",
            OwnerFirstName: draftProvider.OwnerFirstName,
            OwnerLastName: draftProvider.OwnerLastName,
            LogoUrl: draftProvider.Profile.LogoUrl
        );

        // Map location
        var location = new LocationData(
            AddressLine1: draftProvider.Address.Street,
            AddressLine2: null,
            City: draftProvider.Address.City,
            Province: draftProvider.Address.State,
            PostalCode: draftProvider.Address.PostalCode,
            Latitude: (decimal)(draftProvider.Address.Latitude ?? 0),
            Longitude: (decimal)(draftProvider.Address.Longitude ?? 0),
            ProvinceId: draftProvider.Address.ProvinceId,
            CityId: draftProvider.Address.CityId
        );

        // Map services - Load from ServiceRepository (Service is separate aggregate)
        var providerServices = await _serviceRepository
            .GetServicesByProviderIdAsync(draftProvider.Id, cancellationToken);

        var services = providerServices.Select(s => new ServiceData(
            Id: s.Id.Value.ToString(),
            Name: s.Name,
            DurationHours: s.Duration.Value / 60,
            DurationMinutes: s.Duration.Value % 60,
            Price: s.BasePrice.Amount,
            PriceType: s.Type.ToString()
        )).ToList();

      

        // Map business hours
        var businessHours = draftProvider.BusinessHours.Select(bh => new BusinessHoursData(
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
        )).ToList();


        // Map gallery images from provider's business profile
        var galleryImages = draftProvider.Profile.GalleryImages
            .Where(img => img.IsActive)
            .OrderBy(img => img.DisplayOrder)
            .Select(img => new GalleryImageData(
                Id: img.Id.ToString(),
                ImageUrl: _urlService.ToAbsoluteUrl(img.ImageUrl),
                ThumbnailUrl: _urlService.ToAbsoluteUrl(img.ThumbnailUrl),
                MediumUrl: _urlService.ToAbsoluteUrl(img.MediumUrl) ,
                DisplayOrder: img.DisplayOrder,
                IsPrimary: img.IsPrimary,
                Caption: img.Caption,
                AltText: img.AltText,
                UploadedAt: img.UploadedAt
            )).ToList();

        var draftData = new ProviderDraftData(
            ProviderId: draftProvider.Id.Value,
            RegistrationStep: draftProvider.RegistrationStep,
            Status: draftProvider.Status.ToString(),
            BusinessInfo: businessInfo,
            Location: location,
            Services: services,
            BusinessHours: businessHours,
            GalleryImages: galleryImages
        );

        return new GetRegistrationProgressResult(
            HasDraft: true,
            CurrentStep: draftProvider.RegistrationStep,
            ProviderId: draftProvider?.Id.Value,
            DraftData: draftData
        );
    }
}
