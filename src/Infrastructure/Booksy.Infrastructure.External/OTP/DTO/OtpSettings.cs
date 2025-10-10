using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Booksy.Infrastructure.External.OTP.DTO
{
    public class OtpSettings
    {
        public int StepWindow { get; set; }

        public int Size { get; set; }

        public string SecretKey { get; set; }

        public bool Sandbox { get; set; }

        public string SandboxCode { get; set; }
    }
}
