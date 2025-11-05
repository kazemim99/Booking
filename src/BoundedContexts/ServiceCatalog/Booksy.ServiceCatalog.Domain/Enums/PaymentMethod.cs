// ========================================
// Booksy.ServiceCatalog.Domain/Enums/PaymentMethod.cs
// ========================================
namespace Booksy.ServiceCatalog.Domain.Enums
{
    /// <summary>
    /// Represents the payment method used
    /// </summary>
    public enum PaymentMethod
    {
        /// <summary>
        /// Credit or debit card
        /// </summary>
        CreditCard,

        /// <summary>
        /// Bank transfer
        /// </summary>
        BankTransfer,

        /// <summary>
        /// Digital wallet (PayPal, Apple Pay, Google Pay)
        /// </summary>
        Wallet,

        /// <summary>
        /// Cash payment
        /// </summary>
        Cash,

        /// <summary>
        /// Other payment method
        /// </summary>
        Other
    }
}
