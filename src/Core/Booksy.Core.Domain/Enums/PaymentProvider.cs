namespace Booksy.Core.Domain.Enums
{
    /// <summary>
    /// Represents the payment gateway provider
    /// </summary>
    public enum PaymentProvider
    {
        /// <summary>
        /// ZarinPal payment gateway (Iranian gateway)
        /// </summary>
        ZarinPal,

        /// <summary>
        /// IDPay payment gateway (Iranian gateway)
        /// </summary>
        IDPay,

        /// <summary>
        /// Parsian payment gateway (Iranian gateway)
        /// </summary>
        Parsian,

        /// <summary>
        /// Saman payment gateway (Iranian gateway)
        /// </summary>
        Saman,

        /// <summary>
        /// Behpardakht payment gateway (Iranian gateway)
        /// </summary>
        Behpardakht
    }
}
