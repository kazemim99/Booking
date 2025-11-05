using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Services;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetRegistrationProgress;

public sealed class GetRegistrationProgressQueryHandler
    : IQueryHandler<GetRegistrationProgressQuery, GetRegistrationProgressResult>
{
    private readonly IProviderWriteRepository _providerRepository;
    private readonly IServiceWriteRepository _serviceRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetRegistrationProgressQueryHandler(
        IProviderWriteRepository providerRepository,
        IServiceWriteRepository serviceRepository,
        ICurrentUserService currentUserService)
    {
        _providerRepository = providerRepository;
        _serviceRepository = serviceRepository;
        _currentUserService = currentUserService;
    }

    public async Task<GetRegistrationProgressResult> Handle(
        GetRegistrationProgressQuery request,
        CancellationToken cancellationToken)
    {
        var userId = UserId.From(_currentUserService.UserId ??
            throw new UnauthorizedAccessException("User not authenticated"));

        var draftProvider = await _providerRepository
            .GetDraftProviderByOwnerIdAsync(userId, cancellationToken);

        if (draftProvider == null)
        {
            return new GetRegistrationProgressResult(
                HasDraft: false,
                CurrentStep: null,
                DraftData: null);
        }

        // Map business info
        var businessInfo = new BusinessInfoData(
            BusinessName: draftProvider.Profile.BusinessName,
            BusinessDescription: draftProvider.Profile.BusinessDescription ?? "",
            Category: draftProvider.ProviderType.ToString(),
            PhoneNumber: draftProvider.ContactInfo.PrimaryPhone?.Value ?? "",
            Email: draftProvider.ContactInfo.Email?.Value ?? ""
        );

        // Map location
        var location = new LocationData(
            AddressLine1: draftProvider.Address.Street,
            AddressLine2: null,
            City: draftProvider.Address.City,
            Province: draftProvider.Address.State,
            PostalCode: draftProvider.Address.PostalCode,
            Latitude: (decimal)(draftProvider.Address.Latitude ?? 0),
            Longitude: (decimal)(draftProvider.Address.Longitude ?? 0)
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

        // Map staff
        var staff = draftProvider.Staff.Select(s => new StaffData(
            Id: s.Id.ToString(),
            Name: s.FullName,
            Email: s.Email?.Value ?? "",
            PhoneNumber: s.Phone?.Value ?? "",
            Position: s.Role.ToString()
        )).ToList();

        // Map business hours
        var businessHours = draftProvider.BusinessHours.Select(bh => new BusinessHoursData(
            DayOfWeek: (int)bh.DayOfWeek,
            IsOpen: bh.IsOpen,
            OpenTimeHours: bh.OpenTime?.Hour,
            OpenTimeMinutes: bh.OpenTime?.Minute,
            CloseTimeHours: bh.CloseTime?.Hour,
            CloseTimeMinutes: bh.CloseTime?.Minute
        )).ToList();

        // Map gallery images (Provider doesn't have GalleryImages - would need separate repository)
        var galleryImages = new List<GalleryImageData>();

        var draftData = new ProviderDraftData(
            ProviderId: draftProvider.Id.Value,
            RegistrationStep: draftProvider.RegistrationStep,
            Status: draftProvider.Status.ToString(),
            BusinessInfo: businessInfo,
            Location: location,
            Services: services,
            Staff: staff,
            BusinessHours: businessHours,
            GalleryImages: galleryImages
        );

        return new GetRegistrationProgressResult(
            HasDraft: true,
            CurrentStep: draftProvider.RegistrationStep,
            DraftData: draftData
        );
    }
}
