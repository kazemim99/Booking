using Booksy.Infrastructure.External.OTP.DTO;
using OtpNet;
using System.Text;

namespace Booksy.Infrastructure.External.OTP
{
    public class OtpSharpService : OtpServiceBase, IOtpService
    {
        public OtpSharpService(OtpSettings settings) : base(settings)
        {
        }

        public string GetCode(string key, int? stepWindowSeconds = null)
        {
            if (_settings.Sandbox) return _settings.SandboxCode;

            var totp = GetTotp(key);
            return totp.ComputeTotp();
        }

        public OtpVerifyOut VerifyCode(string key, string code)
        {
            if (_settings.Sandbox)
            {
                return new OtpVerifyOut(code == _settings.SandboxCode, 1);
            }
            else
            {
                key = key.TrimStart(new[] { '0' });
                var totp = GetTotp(key);

                var result = totp.VerifyTotp(code, out long timeStepMatched,
                VerificationWindow.RfcSpecifiedNetworkDelay);
                var resp = new OtpVerifyOut(result,
                 (int)(timeStepMatched / 1000));

                if (!resp.Matched || resp.MatchTime <= 0)
                    throw new Exception("کد ارسال شده اشتباه است");

                return resp;
            }
        }

        private Totp GetTotp(string key, int? stepWindowSeconds = null)
        {
            stepWindowSeconds ??= _settings.StepWindow;
            var secretKey = Encoding.UTF8.GetBytes(_settings.SecretKey + key);
            return new Totp(secretKey, stepWindowSeconds.Value, OtpHashMode.Sha256, _settings.Size);
        }
    }
}