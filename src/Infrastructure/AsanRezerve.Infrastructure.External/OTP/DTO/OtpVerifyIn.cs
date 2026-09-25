using System.ComponentModel.DataAnnotations;

namespace AsanRezerve.Infrastructure.External.OTP.DTO
{
    public class OtpVerifyIn
    {
        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string Code { get; set; }
    }
}