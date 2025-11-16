// ========================================
// Booksy.ServiceCatalog.Application/Queries/Provider/GetProviderProfile/GetProviderProfileQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.ServiceCatalog.Application.DTOs.Provider;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderProfile
{
    /// <summary>
    /// Handler for comprehensive provider profile query
    /// Aggregates all data needed for customer-facing profile pages
    /// </summary>
    public sealed class GetProviderProfileQueryHandler : IQueryHandler<GetProviderProfileQuery, ProviderProfileViewModel>
    {
        private readonly IProviderReadRepository _providerRepository;
        private readonly IServiceReadRepository _serviceRepository;
        private readonly IReviewReadRepository _reviewRepository;
        private readonly IBookingReadRepository _bookingRepository;
        private readonly IProviderAvailabilityReadRepository _availabilityRepository;
        private readonly ILogger<GetProviderProfileQueryHandler> _logger;

        public GetProviderProfileQueryHandler(
            IProviderReadRepository providerRepository,
            IServiceReadRepository serviceRepository,
            IReviewReadRepository reviewRepository,
            IBookingReadRepository bookingRepository,
            IProviderAvailabilityReadRepository availabilityRepository,
            ILogger<GetProviderProfileQueryHandler> logger)
        {
            _providerRepository = providerRepository;
            _serviceRepository = serviceRepository;
            _reviewRepository = reviewRepository;
            _bookingRepository = bookingRepository;
            _availabilityRepository = availabilityRepository;
            _logger = logger;
        }

        public async Task<ProviderProfileViewModel> Handle(
            GetProviderProfileQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting comprehensive profile for provider {ProviderId}", request.ProviderId);

            var providerId = ProviderId.From(request.ProviderId);

            // Get provider details
            var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);
            if (provider == null)
            {
                throw new NotFoundException($"Provider with ID {request.ProviderId} not found");
            }

            // Get services
            var services = await _serviceRepository.GetByProviderIdAndStatusAsync(
                providerId,
                ServiceStatus.Active,
                cancellationToken);

            // Get reviews (using pagination with page 1 and ReviewsLimit as pageSize)
            var reviews = await _reviewRepository.GetByProviderIdAsync(
                providerId,
                pageNumber: 1,
                pageSize: request.ReviewsLimit,
                cancellationToken: cancellationToken);

            // Get bookings for statistics (all bookings for the provider, then filter by status)
            var allBookings = await _bookingRepository.GetByProviderIdAsync(
                providerId,
                cancellationToken);
            var completedBookings = allBookings.Where(b => b.Status == BookingStatus.Completed).ToList();

            // Get availability summary for next 7 days
            var startDate = DateTime.UtcNow.Date;
            var endDate = startDate.AddDays(request.AvailabilityDays);
            var availabilitySlots = await _availabilityRepository.GetAvailabilityByDateRangeAsync(
                providerId,
                startDate,
                endDate,
                cancellationToken);

            // Build view model
            var viewModel = new ProviderProfileViewModel
            {
                // Basic Info
                ProviderId = provider.Id.Value,
                BusinessName = provider.Profile.BusinessName,
                Description = provider.Profile.BusinessDescription,
                LogoUrl = provider.Profile.LogoUrl,
                ProfileImageUrl = provider.Profile.ProfileImageUrl,
                Type = provider.ProviderType,
                Status = provider.Status,
                PriceRange = provider.PriceRange,

                // Contact & Location
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

                // Business Settings
                AllowOnlineBooking = provider.AllowOnlineBooking,
                OffersMobileServices = provider.OffersMobileServices,
                IsVerified = provider.Status == ProviderStatus.Verified,

                // Rating & Social Proof
                AverageRating = provider.AverageRating,
                TotalReviews = reviews.TotalCount,
                RecentReviews = reviews.Reviews.Take(request.ReviewsLimit).Select(r => new ReviewSummaryViewModel
                {
                    ReviewId = r.Id,
                    CustomerName = "Anonymous", // TODO: Join with customer data for privacy-safe name
                    Rating = r.RatingValue,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    ServiceName = null // TODO: Join with service data for service name
                }).ToList(),

                // Services
                TotalServices = services.Count,
                Services = services.Take(request.ServicesLimit).Select(s => new ServiceDetailViewModel
                {
                    ServiceId = s.Id.Value,
                    Name = s.Name,
                    Description = s.Description,
                    Category = s.Category.Name,
                    Price = s.BasePrice.Amount,
                    Currency = s.BasePrice.Currency,
                    DurationMinutes = s.Duration.Value,
                    IsPopular = false // TODO: Calculate from booking frequency
                }).ToList(),

                // Staff
                TotalStaff = provider.Staff.Count(s => s.IsActive),
                Staff = provider.Staff
                    .Where(s => s.IsActive)
                    .Select(s => new StaffProfileViewModel
                    {
                        StaffId = s.Id,
                        FullName = s.FullName,
                        Role = s.Role.ToString()
                    }).ToList(),

                // Gallery
                GalleryImages = provider.Profile.GalleryImages
                    .Where(g => g.IsActive)
                    .OrderBy(g => g.DisplayOrder)
                    .Select(g => new GalleryImageViewModel
                    {
                        ImageUrl = g.ImageUrl,
                        Caption = g.Caption,
                        IsPrimary = g.IsPrimary,
                        DisplayOrder = g.DisplayOrder
                    }).ToList(),

                // Business Hours
                BusinessHours = provider.BusinessHours.ToDictionary(
                    bh => bh.DayOfWeek,
                    bh => bh.IsOpen ? new BusinessHoursDto
                    {
                        DayOfWeek = bh.DayOfWeek,
                        IsOpen = bh.IsOpen,
                        OpenTime = bh.OpenTime,
                        CloseTime = bh.CloseTime
                    } : null),

                // Availability Summary
                AvailabilitySummary = new AvailabilitySummaryViewModel
                {
                    NextAvailableSlot = availabilitySlots
                        .Where(slot => slot.Status == AvailabilityStatus.Available &&
                                     slot.Date.Add(slot.StartTime.ToTimeSpan()) > DateTime.UtcNow)
                        .OrderBy(slot => slot.Date)
                        .ThenBy(slot => slot.StartTime)
                        .Select(slot => slot.Date.Add(slot.StartTime.ToTimeSpan()))
                        .FirstOrDefault(),
                    AvailableSlotsNext7Days = availabilitySlots
                        .Count(slot => slot.Status == AvailabilityStatus.Available),
                    AverageAvailabilityPercentage = availabilitySlots.Any()
                        ? (double)availabilitySlots.Count(s => s.Status == AvailabilityStatus.Available) / availabilitySlots.Count * 100
                        : 0
                },

                // Statistics
                Statistics = new ProviderStatsViewModel
                {
                    TotalBookings = completedBookings.Count,
                    CompletedBookings = completedBookings.Count,
                    ResponseRate = 95.0m, // TODO: Calculate from response time data
                    RepeatCustomers = 0 // TODO: Calculate from booking customer IDs
                },

                // Tags & Categories
                Tags = provider.Profile.Tags.ToList(),
                ServiceCategories = services
                    .Select(s => s.Category.Name)
                    .Distinct()
                    .ToList(),

                // Timestamps
                RegisteredAt = provider.RegisteredAt,
                LastActiveAt = provider.LastActiveAt,
                YearsInBusiness = DateTime.UtcNow.Year - provider.RegisteredAt.Year
            };

            _logger.LogInformation(
                "Successfully retrieved comprehensive profile for provider {ProviderId}: {ServiceCount} services, {ReviewCount} reviews, {AvailableSlots} available slots",
                request.ProviderId,
                viewModel.TotalServices,
                viewModel.TotalReviews,
                viewModel.AvailabilitySummary.AvailableSlotsNext7Days);

            return viewModel;
        }
    }
}
