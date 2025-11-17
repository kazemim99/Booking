using System.ComponentModel.DataAnnotations;

namespace Booksy.ServiceCatalog.Api.Models.Requests;

/// <summary>
/// Request model for getting provider availability calendar
/// </summary>
public class GetProviderAvailabilityCalendarRequest
{
    /// <summary>
    /// Start date for availability window (ISO format: yyyy-MM-dd)
    /// Defaults to today if not provided
    /// </summary>
    /// <example>2025-11-20</example>
    public string? StartDate { get; set; }

    /// <summary>
    /// Number of days to fetch (7, 14, or 30)
    /// Defaults to 7 days
    /// </summary>
    /// <example>7</example>
    [Range(7, 30, ErrorMessage = "Days must be 7, 14, or 30")]
    public int Days { get; set; } = 7;
}
