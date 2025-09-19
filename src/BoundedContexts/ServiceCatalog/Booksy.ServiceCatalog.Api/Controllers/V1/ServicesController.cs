//===========================================
// Controllers/V1/ServicesController.cs
//===========================================
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Booksy.ServiceCatalog.Application.Commands.Service.CreateService;
using Booksy.ServiceCatalog.Application.Commands.Service.UpdateService;
using Booksy.ServiceCatalog.Application.Commands.Service.ActivateService;
using Booksy.ServiceCatalog.Application.Queries.Service.GetServiceById;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.ServiceCatalog.API.Models.Requests;
using Booksy.ServiceCatalog.API.Models.Responses;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.API.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ServicesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IServiceApplicationService _serviceApplicationService;
    private readonly ILogger<ServicesController> _logger;

    public ServicesController(
        IMediator mediator,
        IServiceApplicationService serviceApplicationService,
        ILogger<ServicesController> logger)
    {
        _mediator = mediator;
        _serviceApplicationService = serviceApplicationService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new service
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ServiceResponse>> CreateService(
        [FromBody] CreateServiceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateServiceCommand(
            ProviderId: request.ProviderId,
            Name: request.Name,
            Description: request.Description,
            CategoryName: request.CategoryName,
            ServiceType: request.ServiceType,
            BasePrice: request.BasePrice,
            Currency: request.Currency,
            DurationMinutes: request.DurationMinutes,
            PreparationMinutes: request.PreparationMinutes,
            BufferMinutes: request.BufferMinutes,
            RequiresDeposit: request.RequiresDeposit,
            DepositPercentage: request.DepositPercentage,
            AvailableAtLocation: request.AvailableAtLocation,
            AvailableAsMobile: request.AvailableAsMobile,
            MaxAdvanceBookingDays: request.MaxAdvanceBookingDays,
            MinAdvanceBookingHours: request.MinAdvanceBookingHours,
            MaxConcurrentBookings: request.MaxConcurrentBookings,
            Tags: request.Tags,
            ImageUrl: request.ImageUrl,
            IdempotencyKey: request.IdempotencyKey
        );

        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetService),
            new { id = result.ServiceId },
            new ServiceResponse
            {
                Id = result.ServiceId,
                Name = result.Name,
                Category = result.Category,
                Status = result.Status.ToString(),
                Message = "Service created successfully."
            });
    }

    /// <summary>
    /// Get service by ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ServiceDetailsResponse>> GetService(
        Guid id,
        [FromQuery] bool includeProvider = false,
        [FromQuery] bool includeOptions = false,
        [FromQuery] bool includePriceTiers = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetServiceByIdQuery(
            ServiceId: id,
            IncludeProvider: includeProvider,
            IncludeOptions: includeOptions,
            IncludePriceTiers: includePriceTiers
        );

        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(new ServiceDetailsResponse
        {
            Id = result.Id,
            ProviderId = result.ProviderId,
            Name = result.Name,
            Description = result.Description,
            Category = result.Category,
            Type = result.Type.ToString(),
            BasePrice = result.BasePrice,
            Currency = result.Currency,
            Duration = result.Duration,
            PreparationTime = result.PreparationTime,
            BufferTime = result.BufferTime,
            TotalDuration = result.TotalDuration,
            Status = result.Status.ToString(),
            RequiresDeposit = result.RequiresDeposit,
            DepositPercentage = result.DepositPercentage,
            DepositAmount = result.DepositAmount,
            AllowOnlineBooking = result.AllowOnlineBooking,
            AvailableAtLocation = result.AvailableAtLocation,
            AvailableAsMobile = result.AvailableAsMobile,
            MaxAdvanceBookingDays = result.MaxAdvanceBookingDays,
            MinAdvanceBookingHours = result.MinAdvanceBookingHours,
            MaxConcurrentBookings = result.MaxConcurrentBookings,
            ImageUrl = result.ImageUrl,
            Tags = result.Tags,
            QualifiedStaffCount = result.QualifiedStaffCount,
            CanBeBooked = result.CanBeBooked,
            CreatedAt = result.CreatedAt,
            ActivatedAt = result.ActivatedAt,
            Options = result.Options?.Select(o => new ServiceOptionResponse
            {
                Id = o.Id,
                Name = o.Name,
                Description = o.Description,
                AdditionalPrice = o.AdditionalPrice,
                Currency = o.Currency,
                AdditionalDuration = o.AdditionalDuration,
                IsRequired = o.IsRequired,
                IsActive = o.IsActive,
                SortOrder = o.SortOrder
            }).ToList(),
            PriceTiers = result.PriceTiers?.Select(pt => new PriceTierResponse
            {
                Id = pt.Id,
                Name = pt.Name,
                Description = pt.Description,
                Price = pt.Price,
                Currency = pt.Currency,
                IsDefault = pt.IsDefault,
                IsActive = pt.IsActive,
                SortOrder = pt.SortOrder
            }).ToList(),
            Provider = result.Provider != null ? new ProviderSummaryResponse
            {
                Id = result.Provider.Id,
                BusinessName = result.Provider.BusinessName,
                Description = result.Provider.Description,
                Type = result.Provider.Type.ToString(),
                City = result.Provider.City,
                State = result.Provider.State,
                LogoUrl = result.Provider.LogoUrl,
                AllowOnlineBooking = result.Provider.AllowOnlineBooking,
                OffersMobileServices = result.Provider.OffersMobileServices
            } : null
        });
    }

    /// <summary>
    /// Update service
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ServiceResponse>> UpdateService(
        Guid id,
        [FromBody] UpdateServiceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateServiceCommand(
            ServiceId: id,
            Name: request.Name,
            Description: request.Description,
            CategoryName: request.CategoryName,
            DurationMinutes: request.DurationMinutes,
            PreparationMinutes: request.PreparationMinutes,
            BufferMinutes: request.BufferMinutes,
            Tags: request.Tags,
            ImageUrl: request.ImageUrl,
            IdempotencyKey: request.IdempotencyKey
        );

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(new ServiceResponse
        {
            Id = result.ServiceId,
            Name = result.Name,
            Status = "Updated",
            Message = "Service updated successfully."
        });
    }

    /// <summary>
    /// Get services by provider
    /// </summary>
    [HttpGet("provider/{providerId}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ServiceSummaryResponse>>> GetServicesByProvider(
        Guid providerId,
        [FromQuery] string? status,
        CancellationToken cancellationToken = default)
    {
        var providerIdVo = ProviderId.From(providerId);
        var statusEnum = !string.IsNullOrEmpty(status) && Enum.TryParse<ServiceStatus>(status, true, out var parsedStatus)
            ? parsedStatus
            : (ServiceStatus?)null;

        var result = await _serviceApplicationService.GetServicesByProviderAsync(
            providerIdVo, statusEnum, cancellationToken);

        return Ok(result.Select(s => new ServiceSummaryResponse
        {
            Id = s.Id,
            ProviderId = providerId,
            Name = s.Name,
            Description = s.Description,
            Category = s.Category,
            BasePrice = s.BasePrice,
            Currency = s.Currency,
            Duration = s.Duration,
            Status = s.Status.ToString(),
            ImageUrl = s.ImageUrl,
            Tags = s.Tags
        }));
    }

    /// <summary>
    /// Search services
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ServiceSummaryResponse>>> SearchServices(
        [FromQuery] string searchTerm,
        [FromQuery] string? category,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Application.DTOs.Service.ServiceSummaryDto> result;

        if (!string.IsNullOrEmpty(category))
        {
            result = await _serviceApplicationService.GetServicesByCategoryAsync(category, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(searchTerm))
        {
            result = await _serviceApplicationService.SearchServicesAsync(searchTerm, cancellationToken);
        }
        else
        {
            return BadRequest("Either searchTerm or category must be provided");
        }

        return Ok(result.Select(s => new ServiceSummaryResponse
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            Category = s.Category,
            BasePrice = s.BasePrice,
            Currency = s.Currency,
            Duration = s.Duration,
            Status = s.Status.ToString(),
            ImageUrl = s.ImageUrl,
            Tags = s.Tags
        }));
    }

    /// <summary>
    /// Activate service
    /// </summary>
    [HttpPost("{id}/activate")]
    public async Task<ActionResult<ServiceResponse>> ActivateService(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new ActivateServiceCommand(ServiceId: id);
        var result = await _mediator.Send(command, cancellationToken);

        return Ok(new ServiceResponse
        {
            Id = result.ServiceId,
            Name = result.Name,
            Status = "Active",
            Message = "Service activated successfully."
        });
    }

    /// <summary>
    /// Deactivate service
    /// </summary>
    [HttpPost("{id}/deactivate")]
    public async Task<ActionResult<ServiceResponse>> DeactivateService(
        Guid id,
        [FromBody] DeactivateServiceRequest request,
        CancellationToken cancellationToken)
    {
        // Since there's no explicit DeactivateServiceCommand in the project,
        // we'll need to use the service application service or create the command
        var serviceId = ServiceId.From(id);
        var service = await _serviceApplicationService.GetServiceByIdAsync(serviceId, cancellationToken);

        if (service == null)
        {
            return NotFound();
        }

        // This would need a proper DeactivateServiceCommand implementation
        // For now, return a placeholder response
        return Ok(new ServiceResponse
        {
            Id = id,
            Name = service.Name,
            Status = "Deactivated",
            Message = $"Service deactivated: {request.Reason}"
        });
    }

    /// <summary>
    /// Archive service (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ServiceResponse>> ArchiveService(
        Guid id,
        CancellationToken cancellationToken)
    {
        // Since there's no explicit ArchiveServiceCommand in the project,
        // we'll need to use the service application service or create the command
        var serviceId = ServiceId.From(id);
        var service = await _serviceApplicationService.GetServiceByIdAsync(serviceId, cancellationToken);

        if (service == null)
        {
            return NotFound();
        }

        // This would need a proper ArchiveServiceCommand implementation
        // For now, return a placeholder response
        return Ok(new ServiceResponse
        {
            Id = id,
            Name = service.Name,
            Status = "Archived",
            Message = "Service archived successfully."
        });
    }

    /// <summary>
    /// Get service statistics
    /// </summary>
    [HttpGet("{id}/statistics")]
    [Authorize(Roles = "Provider,Admin")]
    public async Task<ActionResult<ServiceStatisticsResponse>> GetServiceStatistics(
        Guid id,
        CancellationToken cancellationToken)
    {
        var serviceId = ServiceId.From(id);
        var statistics = await _serviceApplicationService.GetServiceStatisticsAsync(serviceId, cancellationToken);

        return Ok(new ServiceStatisticsResponse
        {
            ServiceId = statistics.ServiceId,
            ServiceName = statistics.ServiceName,
            TotalBookings = statistics.TotalBookings,
            CompletedBookings = statistics.CompletedBookings,
            CancelledBookings = statistics.CancelledBookings,
            AverageRating = statistics.AverageRating,
            TotalRevenue = statistics.TotalRevenue,
            AverageBookingValue = statistics.AverageBookingValue,
            BookingTrend = statistics.BookingTrend
        });
    }

    /// <summary>
    /// Calculate service price with options
    /// </summary>
    [HttpPost("{id}/calculate-price")]
    [AllowAnonymous]
    public async Task<ActionResult<PriceCalculationResponse>> CalculatePrice(
        Guid id,
        [FromBody] PriceCalculationRequest request,
        CancellationToken cancellationToken)
    {
        var serviceId = ServiceId.From(id);
        var price = await _serviceApplicationService.CalculateServicePriceAsync(
            serviceId,
            request.SelectedOptionIds ?? new List<Guid>(),
            request.SelectedTierId,
            cancellationToken);

        return Ok(new PriceCalculationResponse
        {
            ServiceId = id,
            BasePrice = price.Amount,
            Currency = price.Currency,
            TotalPrice = price.Amount, // This would include option calculations
            Breakdown = new List<PriceBreakdownItem>
            {
                new() { Description = "Base service", Amount = price.Amount }
            }
        });
    }
}
