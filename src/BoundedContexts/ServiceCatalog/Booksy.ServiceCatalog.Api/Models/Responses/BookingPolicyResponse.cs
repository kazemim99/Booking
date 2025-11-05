namespace Booksy.ServiceCatalog.Api.Models.Responses;

/// <summary>
/// Booking policy response model
/// </summary>
public class BookingPolicyResponse
{
    public int MinAdvanceBookingHours { get; set; }
    public int MaxAdvanceBookingDays { get; set; }
    public int CancellationWindowHours { get; set; }
    public decimal CancellationFeePercentage { get; set; }
    public bool AllowRescheduling { get; set; }
    public int RescheduleWindowHours { get; set; }
    public bool RequireDeposit { get; set; }
    public decimal DepositPercentage { get; set; }
}
