namespace Booksy.Core.Domain.ValueObjects
{
    /// <summary>
    /// The one currency this platform trades in. Booksy sells salon services in Iran, where prices
    /// are quoted in Toman, and every amount in the system has always BEEN Toman — the code stamped
    /// on it said "USD", which is how a 1,500,000-Toman haircut reached customers as «USD ۱۵۰۰۰۰۰».
    ///
    /// <para>Toman has no ISO 4217 code of its own (IRR is the Rial, a tenth of a Toman), so "IRT"
    /// is the internal marker; what people read is <see cref="PersianName"/>, written by the apps.</para>
    /// </summary>
    public static class PlatformCurrency
    {
        public const string Code = "IRT";

        public const string PersianName = "تومان";
    }
}
