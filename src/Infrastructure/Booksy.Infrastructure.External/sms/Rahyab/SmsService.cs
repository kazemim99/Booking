
using Booksy.Infrastructure.External.OTP.DTO;
using Booksy.Infrastructure.External.sms;
using Serilog;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Booksy.Infrastructure.External.sms.Rahyab
{
    public class SmsService : IRahyabSmsSender
    {
        private readonly OtpSettings _smsSetting;

        public SmsService(OtpSettings smsSetting)
        {
            _smsSetting = smsSetting;
        }

        public void SendEmail(string code ,string to)
        {
            //var message = new MimeMessage();
            //message.From.Add(new MailboxAddress("Power Box", "powerbox.ir98@gmail.com"));
            //message.To.Add(new MailboxAddress("pritom", to));
            //message.Subject = "کد ورود";
            //message.Body = new TextPart("plain")
            //{
            //    Text = $"کد ورود شما به پاور باکس : {code}",
            //};
            //using (var client = new SmtpClient())
            //{
            //    client.Timeout = 20000;
            //    client.Connect("smtp.gmail.com", 587, false);
            //    client.Authenticate("powerbox.ir98", "xfjsiqpukvgsqzgh");

            //    client.Send(message);
            //    client.Disconnect(true);
            //}
        }


        public async Task SendAsync(string phone ,string code)
        {
            try
            {
                if (_smsSetting.Sandbox)
                {
                    return;
                }
                if (!phone.ValidPhone())
                    throw new Exception($" {phone} نا معتبر است"); ;

                var baseAddres = "https://api.rahyab.ir/";

                HttpClient _client = new HttpClient();
                var result = await _client.PostAsJsonAsync($"{baseAddres}api/Auth/getToken", new
                {
                    username = "web_negahno@NEGAHNO",
                   password = ""
                });
                var token = await result.Content.ReadAsStringAsync();
                _client.DefaultRequestHeaders.Add("authorization", $"Bearer {token}");
                var redss = await _client.PostAsJsonAsync($"{baseAddres}api/v1/SendSMS_Single", new RahyabSendSmsReques
                {

                });


                //if (redss.StatusCode != System.Net.HttpStatusCode.OK)
                //throw new Exception($"خطا در ارسال پیام به {request.destinationAddress}");

                Log.Information(await redss.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
              
            }
        }
    }
}
