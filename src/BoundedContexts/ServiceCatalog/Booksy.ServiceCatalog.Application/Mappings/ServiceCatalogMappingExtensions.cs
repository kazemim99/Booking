// ========================================
// Booksy.ServiceCatalog.Application/Mappings/ServiceCatalogMappingExtensions.cs
// ========================================
using Booksy.ServiceCatalog.Application.DTOs.Provider;
using Booksy.ServiceCatalog.Application.DTOs.Service;
using Booksy.ServiceCatalog.Application.Validators.Common;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Mappings
{
    public static class ServiceCatalogMappingExtensions
    {
        public static ProviderSummaryDto ToSummaryDto(this Provider provider)
        {
            return new ProviderSummaryDto
            {
                Id = provider.Id.Value,
                BusinessName = provider.Profile.BusinessName,
                Description = provider.Profile.BusinessDescription,
                Status = provider.Status,
                Type = provider.Size,
                LogoUrl = provider.Profile.LogoUrl,
                City = provider.Address.City,
                State = provider.Address.State,
                Country = provider.Address.Country,
                AllowOnlineBooking = provider.AllowOnlineBooking,
                OffersMobileServices = provider.OffersMobileServices,
                Tags = provider.Profile.Tags,
                RegisteredAt = provider.RegisteredAt,
                LastActiveAt = provider.LastActiveAt
            };
        }

        public static ServiceSummaryDto ToSummaryDto(this Service service)
        {
            return new ServiceSummaryDto
            {
                Id = service.Id.Value,
                ProviderId = service.ProviderId.Value,
                Name = service.Name,
                Description = service.Description,
                Category = service.Category.Name,
                Type = service.Type,
                BasePrice = service.BasePrice.Amount,
                Currency = service.BasePrice.Currency,
                Duration = service.Duration.Value,
                Status = service.Status,
                RequiresDeposit = service.RequiresDeposit,
                AvailableAsMobile = service.AvailableAsMobile,
                ImageUrl = service.ImageUrl,
               
                CanBeBooked = service.CanBeBooked(),
                CreatedAt = service.CreatedAt
            };
        }

        public static ContactInfoDto ToDto(this Domain.ValueObjects.ContactInfo contactInfo)
        {
            return new ContactInfoDto
            {
                Email = contactInfo.Email.Value,
                PrimaryPhone = contactInfo.PrimaryPhone.Value,
                SecondaryPhone = contactInfo.SecondaryPhone?.Value,
                Website = contactInfo.Website,
                FacebookPage = contactInfo.FacebookPage,
                InstagramHandle = contactInfo.InstagramHandle
            };
        }

        public static BusinessAddressDto ToDto(this BusinessAddress address)
        {
            return new BusinessAddressDto
            {
                Street = address.Street,
                City = address.City,
                State = address.State,
                PostalCode = address.PostalCode,
                Country = address.Country,
                Latitude = address.Latitude,
                Longitude = address.Longitude
            };
        }

        public static Price ToPrice(this PriceRequest request)
        {
            return Price.Create(request.Amount, request.Currency);
        }

        public static Duration ToDuration(this int minutes)
        {
            return Duration.FromMinutes(minutes);
        }

        public static ServiceCategory ToServiceCategory(this string categoryName)
        {
            return ServiceCategory.Create(categoryName);
        }
    }

    // Supporting DTOs for mappings
    public sealed class ContactInfoDto
    {
        public string Email { get; set; } = string.Empty;
        public string PrimaryPhone { get; set; } = string.Empty;
        public string? SecondaryPhone { get; set; }
        public string? Website { get; set; }
        public string? FacebookPage { get; set; }
        public string? InstagramHandle { get; set; }
    }

    public sealed class BusinessAddressDto
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}