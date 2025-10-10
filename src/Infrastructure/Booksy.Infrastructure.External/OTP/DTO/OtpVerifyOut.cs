namespace Booksy.Infrastructure.External.OTP.DTO
{

    public class OtpVerifyOut
    {
        public OtpVerifyOut(bool matched, int matchTime)
        {
            Matched = matched;
            MatchTime = matchTime;
        }

        public bool Matched { get; set; }

        public int MatchTime { get; set; }
    }
}