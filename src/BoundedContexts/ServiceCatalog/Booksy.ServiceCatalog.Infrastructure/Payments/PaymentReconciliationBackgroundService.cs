using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Payments
{
    /// <summary>
    /// Periodically runs <see cref="IPaymentReconciler"/> so stuck Pending payments are
    /// always resolved even if no callback ever arrives (C2, invariants I1/I3). The sweep
    /// is idempotent and self-healing: failures are logged and retried on the next tick.
    /// </summary>
    public sealed class PaymentReconciliationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PaymentReconciliationBackgroundService> _logger;

        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _staleAfter = TimeSpan.FromMinutes(15);

        public PaymentReconciliationBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<PaymentReconciliationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Let the app finish starting before the first sweep.
            try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
            catch (OperationCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var reconciler = scope.ServiceProvider.GetRequiredService<IPaymentReconciler>();
                    var settled = await reconciler.ReconcileStalePendingAsync(_staleAfter, 100, stoppingToken);
                    if (settled > 0)
                        _logger.LogInformation("Payment reconciliation settled {Count} payment(s)", settled);

                    await Task.Delay(_interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Payment reconciliation sweep failed; will retry");
                    try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
                    catch (OperationCanceledException) { break; }
                }
            }
        }
    }
}
