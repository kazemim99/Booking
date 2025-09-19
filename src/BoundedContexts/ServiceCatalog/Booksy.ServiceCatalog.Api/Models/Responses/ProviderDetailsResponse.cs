﻿namespace Booksy.ServiceCatalog.API.Models.Responses;

public class ProviderDetailsResponse
{
    public Guid Id { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public ContactInfoResponse ContactInfo { get; set; } = new();
    public AddressResponse Address { get; set; } = new();
    public Dictionary<DayOfWeek, BusinessHoursResponse>? BusinessHours { get; set; }
    public bool AllowOnlineBooking { get; set; }
    public bool OffersMobileServices { get; set; }
    public string? LogoUrl { get; set; }
    public List<string>? Tags { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? LastActiveAt { get; set; }
}
