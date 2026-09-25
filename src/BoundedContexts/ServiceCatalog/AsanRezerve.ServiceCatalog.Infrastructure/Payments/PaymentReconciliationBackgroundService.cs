using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.ServiceCatalog.Infrastructure.Payments
{
    /// <summary>
    /// Periodically runs <see cref="IPaymentReconciler"/> so stuck Pending payments are
    /// always resolved even if no callback ever arrives (C2, invariants I1/I3). The sweep
    /// is idempotent and self-healing: failures are logged and retried on the next tick.
    /// Gated by <c>Finance:ReconciliationEnabled</c> (default on), the same switch its sibling
    /// <see cref="LedgerMaintenanceBackgroundService"/> already honours — added here so an
    /// integration test host that lives long enough for the +1 minute delay to elapse does not
    /// have this sweeping other tests' payments through the real gateway underneath it.
    /// </summary>
    public sealed class PaymentReconciliationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentReconciliationBackgroundService> _logger;

        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _staleAfter = TimeSpan.FromMinutes(15);

        public PaymentReconciliationBackgroundService(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<PaymentReconciliationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_configuration.GetValue("Finance:ReconciliationEnabled", true))
            {
                _logger.LogInformation("Payment reconciliation disabled (Finance:ReconciliationEnabled=false)");
                return;
            }

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
