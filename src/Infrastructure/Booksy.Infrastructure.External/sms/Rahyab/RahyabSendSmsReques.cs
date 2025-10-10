namespace Booksy.Infrastructure.External.sms.Rahyab
{
    public class RahyabSendSmsReques
    {
        public string message { get; set; }

        public string destinationAddress { get; set; }

        public string number { get; set; } = "1000110110001";

        public string userName { get; set; } = "web_negahno";

        public string password { get; set; } = "B3q71jaY96";

        public string company { get; set; } = "NEGAHNO";

        public string messageId { get; set; }
    }
}