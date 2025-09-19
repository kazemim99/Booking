namespace Booksy.ServiceCatalog.API.Models.Responses;

public class ServiceStatisticsResponse
{
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int TotalBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int CancelledBookings { get; set; }
    public decimal AverageRating { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageBookingValue { get; set; }
    public string BookingTrend { get; set; } = string.Empty;
}
