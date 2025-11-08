// ========================================
// GetPaymentReconciliationQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Payment.GetPaymentReconciliation
{
    public sealed record GetPaymentReconciliationQuery(
        DateTime StartDate,
        DateTime EndDate) : IQuery<ReconciliationReportViewModel>;
}
