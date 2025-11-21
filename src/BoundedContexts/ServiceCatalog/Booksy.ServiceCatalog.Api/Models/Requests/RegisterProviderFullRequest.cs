//===========================================
// Models/Requests/RegisterProviderFullRequest.cs
// Complete provider registration with all multi-step data
//===========================================

using System.ComponentModel.DataAnnotations;
using Booksy.ServiceCatalog.Api.Models.Requests;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.API.Models.Requests;

/// <summary>
/// Complete provider registration request containing all multi-step form data
/// </summary>
public sealed class RegisterProviderFullRequest
{
    /// <summary>
    /// User ID of the provider owner (from authenticated user)
    /// </summary>
    [Required(ErrorMessage = "Owner ID is required")]
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Business category ID
    /// </summary>
    [Required(ErrorMessage = "Business category is required")]
    [StringLength(100)]
    public string CategoryId { get; set; } = string.Empty;

    /// <summary>
    /// Business information
    /// </summary>
    [Required(ErrorMessage = "Business information is required")]
    public BusinessInfoRequest BusinessInfo { get; set; } = new();

    /// <summary>
    /// Business address
    /// </summary>
    [Required(ErrorMessage = "Address is required")]
    public AddressRequest Address { get; set; } = new();

    /// <summary>
    /// Business location (coordinates)
    /// </summary>
    public BusinessLocationRequest? Location { get; set; }

    /// <summary>
    /// Business operating hours (0 = Sunday, 1 = Monday, ..., 6 = Saturday)
    /// </summary>
    [Required(ErrorMessage = "Business hours are required")]
    [MinLength(1, ErrorMessage = "At least one working day is required")]
    public Dictionary<int, RegistrationDayScheduleRequest?> BusinessHours { get; set; } = new();

    /// <summary>
    /// Services offered by the provider
    /// </summary>
    [Required(ErrorMessage = "Services are required")]
    [MinLength(1, ErrorMessage = "At least one service is required")]
    public List<RegistrationServiceRequest> Services { get; set; } = new();

    /// <summary>
    /// Assistance options selected by provider
    /// </summary>
    public List<string> AssistanceOptions { get; set; } = new();

    /// <summary>
    /// Team members (staff)
    /// </summary>
    public List<TeamMemberRequest> TeamMembers { get; set; } = new();
    public string BusinessName { get; set; }
    public string Description { get; set; }
    public ProviderType ProviderType { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Street { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string PostalCode { get; set; }
    public string Country { get; set; }
}

/// <summary>
/// Business information from step 2
/// </summary>
public sealed class BusinessInfoRequest
{
    [Required(ErrorMessage = "Business name is required")]
    [StringLength(200, MinimumLength = 2)]
    public string BusinessName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Owner first name is required")]
    [StringLength(100)]
    public string OwnerFirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Owner last name is required")]
    [StringLength(100)]
    public string OwnerLastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required")]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;
}

/// <summary>
/// Business location with coordinates
/// </summary>
public sealed class BusinessLocationRequest
{
    [Required]
    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Required]
    [Range(-180, 180)]
    public double Longitude { get; set; }

    [StringLength(500)]
    public string? FormattedAddress { get; set; }
}






/// <summary>
/// Service offering in registration flow
/// </summary>
public sealed class RegistrationServiceRequest
{
    [Required(ErrorMessage = "Service name is required")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int DurationHours { get; set; }

    [Range(0, 59)]
    public int DurationMinutes { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Required]
    [RegularExpression("^(fixed|variable)$")]
    public string PriceType { get; set; } = "fixed";
}

/// <summary>
/// Team member/staff
/// </summary>
public sealed class TeamMemberRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(10)]
    public string CountryCode { get; set; } = "+1";

    [Required]
    [StringLength(100)]
    public string Position { get; set; } = string.Empty;

    public bool IsOwner { get; set; } = false;
}
