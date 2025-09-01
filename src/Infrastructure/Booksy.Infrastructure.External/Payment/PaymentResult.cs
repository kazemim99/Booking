namespace Booksy.Infrastructure.External.Payment
{
    public class PaymentResult
    {
        public bool IsSuccessful { get; set; }
        public string PaymentId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public string? ErrorCode { get; set; }
    }
}