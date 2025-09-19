namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderById
{
    public sealed class StaffViewModel
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public StaffRole Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime HiredAt { get; set; }
    }
}