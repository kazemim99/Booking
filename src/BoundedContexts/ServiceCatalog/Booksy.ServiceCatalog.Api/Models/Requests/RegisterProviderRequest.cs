//===========================================
// Models/Requests/RegisterProviderRequest.cs
//===========================================

namespace Booksy.ServiceCatalog.API.Models.Requests;

public class RegisterProviderRequest
{
    [Required]
    public Guid OwnerId { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string BusinessName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public ProviderType Type { get; set; }

    [Required]
    public ContactInfoRequest ContactInfo { get; set; } = new();

    [Required]
    public AddressRequest Address { get; set; } = new();

    public Dictionary<DayOfWeek, BusinessHoursRequest>? BusinessHours { get; set; }

    public bool AllowOnlineBooking { get; set; } = true;

    public bool OffersMobileServices { get; set; } = false;

    [Url]
    public string? LogoUrl { get; set; }

    public List<string>? Tags { get; set; }
}
