//using Booksy.ServiceCatalog.API.Models.Requests;
//using Booksy.ServiceCatalog.API.Models.Responses;
//using Booksy.ServiceCatalog.Application.Commands.Provider.RegisterProvider;
//using MediatR;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using static System.Net.Mime.MediaTypeNames;

//namespace Booksy.ServiceCatalog.API.Controllers.V1;

//[ApiController]
//[Route("api/v1/[controller]")]
//[Authorize]
//public class ProvidersController : ControllerBase
//{
//    private readonly IMediator _mediator;
//    private readonly ILogger<ProvidersController> _logger;

//    public ProvidersController(IMediator mediator, ILogger<ProvidersController> logger)
//    {
//        _mediator = mediator;
//        _logger = logger;
//    }

//    /// <summary>
//    /// Register a new service provider
//    /// </summary>
//    [HttpPost]
//    [AllowAnonymous]
//    public async Task<ActionResult<ProviderResponse>> RegisterProvider(
//        [FromBody] RegisterProviderRequest request,
//        CancellationToken cancellationToken)
//    {
//        var command = new RegisterProviderCommand
//        {
//            OwnerId = request.OwnerId,
//            BusinessName = request.BusinessName,
//            Description = request.Description,
//            Type = request.Type,
//            Email = request.ContactInfo.Email,
//            PrimaryPhone = request.ContactInfo.PrimaryPhone,
//            SecondaryPhone = request.ContactInfo.SecondaryPhone,
//            Website = request.ContactInfo.Website,
//            Street = request.Address.Street,
//            City = request.Address.City,
//            State = request.Address.State,
//            PostalCode = request.Address.PostalCode,
//            Country = request.Address.Country,
//            Latitude = request.Address.Latitude,
//            Longitude = request.Address.Longitude,
//            BusinessHours = request.BusinessHours?.ToDictionary(
//                kvp => kvp.Key,
//                kvp => new Application.DTOs.Provider.BusinessHoursDto
//                {
//                    IsOpen = kvp.Value.IsOpen,
//                    OpenTime = kvp.Value.OpenTime,
//                    CloseTime = kvp.Value.CloseTime,
//                    BreakStart = kvp.Value.BreakStart,
//                    BreakEnd = kvp.Value.BreakEnd
//                }),
//            AllowOnlineBooking = request.AllowOnlineBooking,
//            OffersMobileServices = request.OffersMobileServices,
//            LogoUrl = request.LogoUrl,
//            Tags = request.Tags
//        };

//        var result = await _mediator.Send(command, cancellationToken);

//        if (!result.IsSuccess)
//        {
//            return BadRequest(new ErrorResult(result.Error));
//        }

//        return CreatedAtAction(
//            nameof(GetProvider),
//            new { id = result.Data.Id },
//            new ProviderResponse
//            {
//                Id = result.Data.Id,
//                BusinessName = result.Data.BusinessName,
//                Status = result.Data.Status.ToString(),
//                Message = "Provider registered successfully. Activation pending."
//            });
//    }

//    /// <summary>
//    /// Get provider by ID
//    /// </summary>
//    [HttpGet("{id}")]
//    public async Task<ActionResult<ProviderDetailsResponse>> GetProvider(
//        Guid id,
//        CancellationToken cancellationToken)
//    {
//        var query = new GetProviderByIdQuery { ProviderId = ProviderId.From(id) };
//        var result = await _mediator.Send(query, cancellationToken);

//        if (result == null)
//        {
//            return NotFound();
//        }

//        return Ok(new ProviderDetailsResponse
//        {
//            Id = result.Id,
//            BusinessName = result.BusinessName,
//            Description = result.Description,
//            Type = result.Type.ToString(),
//            Status = result.Status.ToString(),
//            ContactInfo = new ContactInfoResponse
//            {
//                Email = result.ContactInfo.Email,
//                PrimaryPhone = result.ContactInfo.PrimaryPhone,
//                SecondaryPhone = result.ContactInfo.SecondaryPhone,
//                Website = result.ContactInfo.Website
//            },
//            Address = new AddressResponse
//            {
//                Street = result.Address.Street,
//                City = result.Address.City,
//                State = result.Address.State,
//                PostalCode = result.Address.PostalCode,
//                Country = result.Address.Country,
//                Latitude = result.Address.Latitude,
//                Longitude = result.Address.Longitude
//            },
//            BusinessHours = result.BusinessHours?.ToDictionary(
//                kvp => kvp.Key,
//                kvp => new BusinessHoursResponse
//                {
//                    IsOpen = kvp.Value.IsOpen,
//                    OpenTime = kvp.Value.OpenTime,
//                    CloseTime = kvp.Value.CloseTime,
//                    BreakStart = kvp.Value.BreakStart,
//                    BreakEnd = kvp.Value.BreakEnd
//                }),
//            AllowOnlineBooking = result.AllowOnlineBooking,
//            OffersMobileServices = result.OffersMobileServices,
//            LogoUrl = result.LogoUrl,
//            Tags = result.Tags,
//            RegisteredAt = result.RegisteredAt,
//            LastActiveAt = result.LastActiveAt
//        });
//    }

//    /// <summary>
//    /// Update provider information
//    /// </summary>
//    [HttpPut("{id}")]
//    public async Task<ActionResult<ProviderResponse>> UpdateProvider(
//        Guid id,
//        [FromBody] UpdateProviderRequest request,
//        CancellationToken cancellationToken)
//    {
//        var command = new UpdateProviderCommand
//        {
//            ProviderId = ProviderId.From(id),
//            BusinessName = request.BusinessName,
//            Description = request.Description,
//            Email = request.ContactInfo?.Email,
//            PrimaryPhone = request.ContactInfo?.PrimaryPhone,
//            SecondaryPhone = request.ContactInfo?.SecondaryPhone,
//            Website = request.ContactInfo?.Website,
//            Street = request.Address?.Street,
//            City = request.Address?.City,
//            State = request.Address?.State,
//            PostalCode = request.Address?.PostalCode,
//            Country = request.Address?.Country,
//            Latitude = request.Address?.Latitude,
//            Longitude = request.Address?.Longitude,
//            BusinessHours = request.BusinessHours?.ToDictionary(
//                kvp => kvp.Key,
//                kvp => new Application.DTOs.Provider.BusinessHoursDto
//                {
//                    IsOpen = kvp.Value.IsOpen,
//                    OpenTime = kvp.Value.OpenTime,
//                    CloseTime = kvp.Value.CloseTime,
//                    BreakStart = kvp.Value.BreakStart,
//                    BreakEnd = kvp.Value.BreakEnd
//                }),
//            AllowOnlineBooking = request.AllowOnlineBooking,
//            OffersMobileServices = request.OffersMobileServices,
//            LogoUrl = request.LogoUrl,
//            Tags = request.Tags
//        };

//        var result = await _mediator.Send(command, cancellationToken);

//        if (!result.IsSuccess)
//        {
//            return BadRequest(new ErrorResult(result.Error));
//        }

//        return Ok(new ProviderResponse
//        {
//            Id = result.Data.Id,
//            BusinessName = result.Data.BusinessName,
//            Status = result.Data.Status.ToString(),
//            Message = "Provider updated successfully."
//        });
//    }

//    /// <summary>
//    /// Search providers by various criteria
//    /// </summary>
//    [HttpGet("search")]
//    [AllowAnonymous]
//    public async Task<ActionResult<PagedResult<ProviderSummaryResponse>>> SearchProviders(
//        [FromQuery] string? searchTerm,
//        [FromQuery] string? type,
//        [FromQuery] string? city,
//        [FromQuery] string? state,
//        [FromQuery] bool? allowsOnlineBooking,
//        [FromQuery] bool? offersMobileServices,
//        [FromQuery] int page = 1,
//        [FromQuery] int pageSize = 20,
//        CancellationToken cancellationToken = default)
//    {
//        var query = new SearchProvidersQuery
//        {
//            SearchTerm = searchTerm,
//            Type = type,
//            City = city,
//            State = state,
//            AllowsOnlineBooking = allowsOnlineBooking,
//            OffersMobileServices = offersMobileServices,
//            Page = page,
//            PageSize = pageSize
//        };

//        var result = await _mediator.Send(query, cancellationToken);

//        return Ok(new PagedResult<ProviderSummaryResponse>
//        {
//            Items = result.Items.Select(p => new ProviderSummaryResponse
//            {
//                Id = p.Id,
//                BusinessName = p.BusinessName,
//                Description = p.Description,
//                Type = p.Type.ToString(),
//                Status = p.Status.ToString(),
//                City = p.City,
//                State = p.State,
//                Country = p.Country,
//                LogoUrl = p.LogoUrl,
//                AllowOnlineBooking = p.AllowOnlineBooking,
//                OffersMobileServices = p.OffersMobileServices,
//                Tags = p.Tags
//            }).ToList(),
//            TotalCount = result.TotalCount,
//            Page = result.Page,
//            PageSize = result.PageSize,
//            TotalPages = result.TotalPages
//        });
//    }

//    /// <summary>
//    /// Get providers by location within specified radius
//    /// </summary>
//    [HttpGet("location")]
//    [AllowAnonymous]
//    public async Task<ActionResult<IEnumerable<ProviderLocationResponse>>> GetProvidersByLocation(
//        [FromQuery] double latitude,
//        [FromQuery] double longitude,
//        [FromQuery] double radiusKm = 10.0,
//        [FromQuery] int limit = 50,
//        CancellationToken cancellationToken = default)
//    {
//        var query = new GetProvidersByLocationQuery
//        {
//            Latitude = latitude,
//            Longitude = longitude,
//            RadiusKm = radiusKm,
//            Limit = limit
//        };

//        var result = await _mediator.Send(query, cancellationToken);

//        return Ok(result.Select(p => new ProviderLocationResponse
//        {
//            Id = p.Id,
//            BusinessName = p.BusinessName,
//            Description = p.Description,
//            Type = p.Type.ToString(),
//            Address = new AddressResponse
//            {
//                Street = p.Address.Street,
//                City = p.Address.City,
//                State = p.Address.State,
//                PostalCode = p.Address.PostalCode,
//                Country = p.Address.Country,
//                Latitude = p.Address.Latitude,
//                Longitude = p.Address.Longitude
//            },
//            DistanceKm = p.DistanceKm,
//            LogoUrl = p.LogoUrl,
//            AllowOnlineBooking = p.AllowOnlineBooking,
//            OffersMobileServices = p.OffersMobileServices
//        }));
//    }

//    /// <summary>
//    /// Activate provider
//    /// </summary>
//    [HttpPost("{id}/activate")]
//    [Authorize(Roles = "Admin")]
//    public async Task<ActionResult<ProviderResponse>> ActivateProvider(
//        Guid id,
//        CancellationToken cancellationToken)
//    {
//        var command = new ActivateProviderCommand { ProviderId = ProviderId.From(id) };
//        var result = await _mediator.Send(command, cancellationToken);

//        if (!result.IsSuccess)
//        {
//            return BadRequest(new ErrorResult(result.Error));
//        }

//        return Ok(new ProviderResponse
//        {
//            Id = result.Data.Id,
//            BusinessName = result.Data.BusinessName,
//            Status = result.Data.Status.ToString(),
//            Message = "Provider activated successfully."
//        });
//    }

//    /// <summary>
//    /// Deactivate provider
//    /// </summary>
//    [HttpPost("{id}/deactivate")]
//    [Authorize(Roles = "Admin")]
//    public async Task<ActionResult<ProviderResponse>> DeactivateProvider(
//        Guid id,
//        [FromBody] DeactivateProviderRequest request,
//        CancellationToken cancellationToken)
//    {
//        var command = new DeactivateProviderCommand
//        {
//            ProviderId = ProviderId.From(id),
//            Reason = request.Reason
//        };

//        var result = await _mediator.Send(command, cancellationToken);

//        if (!result.IsSuccess)
//        {
//            return BadRequest(new ErrorResult(result.Error));
//        }

//        return Ok(new ProviderResponse
//        {
//            Id = result.Data.Id,
//            BusinessName = result.Data.BusinessName,
//            Status = result.Data.Status.ToString(),
//            Message = "Provider deactivated successfully."
//        });
//    }
//}

////===========================================
//// Controllers/V1/ServicesController.cs
////===========================================
//using MediatR;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Booksy.ServiceCatalog.Application.Commands.CreateService;
//using Booksy.ServiceCatalog.Application.Commands.UpdateService;
//using Booksy.ServiceCatalog.Application.Commands.DeleteService;
//using Booksy.ServiceCatalog.Application.Commands.ActivateService;
//using Booksy.ServiceCatalog.Application.Commands.DeactivateService;
//using Booksy.ServiceCatalog.Application.Queries.GetServiceById;
//using Booksy.ServiceCatalog.Application.Queries.GetServicesByProvider;
//using Booksy.ServiceCatalog.Application.Queries.SearchServices;
//using Booksy.ServiceCatalog.API.Models.Requests;
//using Booksy.ServiceCatalog.API.Models.Responses;
//using Booksy.ServiceCatalog.Domain.ValueObjects;
//using Booksy.Core.Application.DTOs;
