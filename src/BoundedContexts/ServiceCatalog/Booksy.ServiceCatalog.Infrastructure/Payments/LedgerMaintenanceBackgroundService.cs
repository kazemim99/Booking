using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Payments
{
    /// <summary>
    /// C5 background settlement maintenance. Periodically (a) reconciles the ledger against captured payments —
    /// posting any charge entry lost to a partial failure — and (b) verifies the double-entry balance invariant,
    /// alerting on drift. The repo has no Hangfire; this uses a <see cref="BackgroundService"/> (consistent with the
    /// C2 payment reconciler) — documented in ARCHITECTURAL_DECISIONS.md. Gated by <c>Finance:ReconciliationEnabled</c>.
    /// </summary>
    public sealed class LedgerMaintenanceBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LedgerMaintenanceBackgroundService> _logger;

        private readonly TimeSpan _interval = TimeSpan.FromMinutes(10);

        public LedgerMaintenanceBackgroundService(
            IServiceProvider serviceProvider, IConfiguration configuration, ILogger<LedgerMaintenanceBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_configuration.GetValue("Finance:ReconciliationEnabled", true))
            {
                _logger.LogInformation("Ledger maintenance disabled (Finance:ReconciliationEnabled=false)");
                return;
            }

            try { await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); }
            catch (OperationCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var reconciler = scope.ServiceProvider.GetRequiredService<ILedgerReconciler>();

                    var recovered = await reconciler.ReconcileMissingChargeEntriesAsync(cancellationToken: stoppingToken);
                    if (recovered > 0)
                        _logger.LogWarning("Ledger maintenance recovered {Count} missing charge entrie(s)", recovered);

                    var drift = await reconciler.DetectBalanceDriftAsync(stoppingToken);
                    if (drift != 0m)
                        _logger.LogError("Ledger maintenance detected balance drift of {Drift}", drift);

                    await Task.Delay(_interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ledger maintenance sweep failed; will retry");
                    try { await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); }
                    catch (OperationCanceledException) { break; }
                }
            }
        }
    }
}
