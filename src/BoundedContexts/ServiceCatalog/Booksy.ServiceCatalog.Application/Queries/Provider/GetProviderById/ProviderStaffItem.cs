namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderById
{
    /// <summary>
    /// Staff member item in GetProviderById result
    /// </summary>
    public sealed class ProviderStaffItem
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