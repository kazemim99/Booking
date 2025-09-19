// ========================================
// Booksy.ServiceCatalog.Application/Queries/Service/GetServiceById/ServiceOptionViewModel.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Queries.Service.GetServiceById
{
    public sealed class ServiceOptionViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal AdditionalPrice { get; set; }
        public string Currency { get; set; } = string.Empty;
        public int? AdditionalDuration { get; set; }
        public bool IsRequired { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
    }
}