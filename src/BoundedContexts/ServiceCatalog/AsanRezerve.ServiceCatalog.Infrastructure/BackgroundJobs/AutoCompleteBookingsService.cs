using AsanRezerve.Core.Application.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.ServiceCatalog.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Runs <see cref="AutoCompleteBookingsJob"/> every fifteen minutes: a booking completes by itself within a quarter
    /// of an hour of its twelve hours being up, which nobody can tell from exactly on time.
    /// </summary>
    /// <remarks>
    /// Gated by <c>Bookings:AutoCompletionEnabled</c> (default on). Off under test (appsettings.Testing.json): a pass
    /// racing the integration tests would complete bookings they arrange as confirmed-and-past. The tests run the job
    /// themselves, at the moment they choose.
    /// </remarks>
    public sealed class AutoCompleteBookingsService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

        /// <summary>Back off on failure rather than retry at full speed against something that is down.</summary>
        private static readonly TimeSpan PauseAfterFailure = TimeSpan.FromMinutes(30);

        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AutoCompleteBookingsService> _logger;

        public AutoCompleteBookingsService(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<AutoCompleteBookingsService> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_configuration.GetValue("Bookings:AutoCompletionEnabled", true))
            {
                _logger.LogInformation("Booking auto-completion disabled (Bookings:AutoCompletionEnabled=false)");
                return;
            }

            _logger.LogInformation("AutoCompleteBookingsService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var job = scope.ServiceProvider.GetRequiredService<AutoCompleteBookingsJob>();
                        var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
                        await job.ExecuteAsync(clock, stoppingToken);
                    }

                    await Task.Delay(Interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Booking auto-completion pass failed; pausing before the next one");

                    try
                    {
                        await Task.Delay(PauseAfterFailure, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }

            _logger.LogInformation("AutoCompleteBookingsService stopped");
        }
    }
}
