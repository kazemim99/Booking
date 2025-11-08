// ========================================
// BehpardakhtSettings.cs
// ========================================
namespace Booksy.Infrastructure.External.Payment.Behpardakht
{
    /// <summary>
    /// Behpardakht (Bank Mellat) payment gateway settings
    /// </summary>
    public class BehpardakhtSettings
    {
        public long TerminalId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserPassword { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://bpm.shaparak.ir/pgwchannel/services/pgw?wsdl";
        public string PaymentPageUrl { get; set; } = "https://bpm.shaparak.ir/pgwchannel/startpay.mellat";
        public string CallbackUrl { get; set; } = string.Empty;
    }
}
