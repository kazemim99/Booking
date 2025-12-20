namespace Booksy.ServiceCatalog.Api.Models.Requests
{
    public class CreateServiceRequest
    {
        public string ServiceName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DurationHours { get; set; } = 0;
        public int DurationMinutes { get; set; }
        public decimal Price { get; set; }
        public string? Currency { get; set; }
        public string? Category { get; set; }
        public bool IsMobileService { get; set; }

        // Aliases for backward compatibility
        public int Duration { get => DurationMinutes; set => DurationMinutes = value; }
        public decimal BasePrice { get => Price; set => Price = value; }
    }

    // Alias for backward compatibility with tests
    public class AddServiceRequest : CreateServiceRequest
    {
    }
}