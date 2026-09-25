namespace AsanRezerve.ServiceCatalog.Domain.ValueObjects
{
    /// <summary>
    /// The discount a booking received, as it was at the moment of booking. A booking keeps this snapshot whatever
    /// happens to the promotion later (edited, paused, ended).
    /// </summary>
    public sealed record AppliedDiscount(
        Guid PromotionId,
        string Title,
        string? Code,
        PromotionOwner Owner,
        decimal Amount);
}
