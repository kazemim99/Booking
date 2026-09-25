using AsanRezerve.Core.Application.Abstractions.Services;
using AsanRezerve.ServiceCatalog.Application.Commands.Booking.AutoCompleteBooking;
using AsanRezerve.ServiceCatalog.Domain.Policies;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.ServiceCatalog.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Completes the confirmed bookings the salon left unmarked, 12 hours after their end
    /// (openspec/changes/_inline/reviews-and-reschedule-round2 D3), so their customers can review them.
    /// </summary>
    /// <remarks>
    /// <para>Nothing in the business happens at "12 hours after the end", so — like the daily digest — a job goes
    /// looking for it. Each booking is completed by its own <see cref="AutoCompleteBookingCommand"/> in its own scope:
    /// its own transaction and change tracker, so one that fails (a concurrency conflict with the salon marking it at
    /// that moment) neither rolls back nor poisons the rest.</para>
    ///
    /// <para>Several hosts may run this at once. The command re-checks the booking and does nothing when it is no longer
    /// due; the booking's concurrency token turns a true race into one failure logged here, retried next pass.</para>
    /// </remarks>
    public sealed class AutoCompleteBookingsJob
    {
        /// <summary>At most this many per pass; the rest wait for the next pass, oldest first.</summary>
        public const int BatchSize = 200;

        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<AutoCompleteBookingsJob> _logger;

        public AutoCompleteBookingsJob(IServiceScopeFactory scopes, ILogger<AutoCompleteBookingsJob> logger)
        {
            _scopes = scopes;
            _logger = logger;
        }

        /// <summary>A pass at the system clock's now.</summary>
        public Task<int> ExecuteAsync(IDateTimeProvider clock, CancellationToken cancellationToken = default) =>
            RunAsync(clock.UtcNow, cancellationToken);

        /// <summary>
        /// One pass at an explicit moment — public so what is due at a given time can be tested without waiting for it.
        /// </summary>
        /// <returns>How many bookings this pass completed.</returns>
        public async Task<int> RunAsync(DateTime utcNow, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Domain.ValueObjects.BookingId> due;
            using (var scope = _scopes.CreateScope())
            {
                due = await scope.ServiceProvider.GetRequiredService<IBookingReadRepository>()
                    .GetConfirmedEndedByAsync(BookingAutoCompletion.LatestDueEnd(utcNow), BatchSize, cancellationToken);
            }

            var completed = 0;
            foreach (var id in due)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    using var scope = _scopes.CreateScope();
                    var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                    if (await sender.Send(new AutoCompleteBookingCommand(id.Value, utcNow), cancellationToken))
                        completed++;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Booking {BookingId} could not be completed automatically; next pass retries", id);
                }
            }

            if (completed > 0)
                _logger.LogInformation("Completed {Count} bookings automatically", completed);

            return completed;
        }
    }
}
