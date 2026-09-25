// ========================================
// GetPaymentReconciliationQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Payment.GetPaymentReconciliation
{
    public sealed record GetPaymentReconciliationQuery(
        DateTime StartDate,
        DateTime EndDate) : IQuery<ReconciliationReportViewModel>;
}
