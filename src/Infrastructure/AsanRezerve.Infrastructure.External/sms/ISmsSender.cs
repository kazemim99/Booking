using AsanRezerve.Infrastructure.External.sms.Rahyab;

namespace AsanRezerve.Infrastructure.External.sms
{
    public interface IRahyabSmsSender
    {
        public Task SendAsync(string phone,string code);
        void SendEmail(string code,string to);

    }
}