using Booksy.Infrastructure.External.OTP.DTO;

namespace Booksy.Infrastructure.External.OTP
{
    public abstract class OtpServiceBase
    {
        protected OtpSettings _settings;

        public bool Sandbox { get; set; }

        public OtpServiceBase(OtpSettings settings)
        {
            _settings = settings;
            Sandbox = _settings.Sandbox;
        }
    }
}