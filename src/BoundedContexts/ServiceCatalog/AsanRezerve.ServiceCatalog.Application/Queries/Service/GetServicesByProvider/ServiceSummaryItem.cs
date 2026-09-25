public sealed class ServiceSummaryItem
{
    public Guid Id { get; init; }
    public Guid ProviderId { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public string Category { get; init; }
    public string Type { get; init; }
    public decimal BasePrice { get; init; }
    public string Currency { get; init; }
    public int Duration { get; init; }
    public string Status { get; init; }
    public bool RequiresDeposit { get; init; }
    public bool AvailableAsMobile { get; init; }
    public string? ImageUrl { get; init; }

    public ServiceSummaryItem(
        Guid id,
        Guid providerId,
        string name,
        string description,
        string category,
        string type,
        decimal basePrice,
        string currency,
        int duration,
        string status,
        bool requiresDeposit,
        bool availableAsMobile,
        string? imageUrl)
    {
        Id = id;
        ProviderId = providerId;
        Name = name;
        Description = description;
        Category = category;
        Type = type;
        BasePrice = basePrice;
        Currency = currency;
        Duration = duration;
        Status = status;
        RequiresDeposit = requiresDeposit;
        AvailableAsMobile = availableAsMobile;
        ImageUrl = imageUrl;
    }
}
