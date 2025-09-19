// ========================================
// Booksy.ServiceCatalog.Application/Queries/Provider/GetProviderById/BusinessHoursViewModel.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderById
{
    public sealed class BusinessHoursViewModel
    {
        public DayOfWeek DayOfWeek { get; set; }
        public bool IsOpen { get; set; }
        public TimeOnly? OpenTime { get; set; }
        public TimeOnly? CloseTime { get; set; }
    }
}