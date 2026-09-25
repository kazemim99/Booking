using AsanRezerve.Infrastructure.External.OTP.DTO;

namespace AsanRezerve.Infrastructure.External.OTP
{
    public interface IOtpService
    {
        bool Sandbox { get; set; }

        string GetCode(string key, int? stepWindowSeconds = null);

        OtpVerifyOut VerifyCode(string key, string code);
    }
}