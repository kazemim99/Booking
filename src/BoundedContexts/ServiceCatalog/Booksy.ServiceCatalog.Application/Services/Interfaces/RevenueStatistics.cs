
namespace Booksy.ServiceCatalog.Application.Services.Interfaces
{

    public sealed class RevenueStatistics
    {
        public decimal TotalRevenue { get; set; }
        public decimal AverageRevenue { get; set; }
        public int TotalTransactions { get; set; }
    }
}