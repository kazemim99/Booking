using Booksy.Core.Application.Abstractions.Idempotency;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Idempotency
{
    /// <summary>
    /// Atomic idempotency store backed by the <c>IdempotencyReservations</c> table's composite PK. Every operation
    /// runs in its <b>own</b> DbContext/transaction (a fresh scope) so the reservation is committed immediately and
    /// visible to concurrent requests, and so complete/release are never entangled with a failed handler's context.
    /// </summary>
    public sealed class IdempotencyStore : IIdempotencyStore
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public IdempotencyStore(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

        public async Task<IdempotencyOutcome> TryReserveAsync(
            string requestType, string key, TimeSpan staleAfter, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();

            // 1) Optimistically try to claim the key with an INSERT. The composite PK makes exactly one concurrent
            //    INSERT win; the rest throw a unique/PK violation.
            var reserved = await TryInsertAsync(db, requestType, key, cancellationToken);
            if (reserved)
                return new IdempotencyOutcome(IdempotencyState.Reserved, null);

            // 2) The key is taken — inspect the existing reservation.
            db.ChangeTracker.Clear();
            var existing = await db.IdempotencyReservations.AsNoTracking()
                .FirstOrDefaultAsync(r => r.RequestType == requestType && r.Key == key, cancellationToken);

            if (existing is null)
                // Raced with a release/reclaim between INSERT-fail and re-read; try once more to claim.
                return await TryInsertAsync(db, requestType, key, cancellationToken)
                    ? new IdempotencyOutcome(IdempotencyState.Reserved, null)
                    : new IdempotencyOutcome(IdempotencyState.InFlight, null);

            if (existing.Status == IdempotencyReservation.Completed)
                return new IdempotencyOutcome(IdempotencyState.Completed, existing.ResultJson);

            // In-flight. Attempt to reclaim it IF stale (the processor crashed) — evaluated entirely against the DB
            // clock (never a .NET DateTime, which is unreliable across DateTime.Kind / timezone). The DELETE removes
            // the row only if it is still older than the staleness window; a fresh reclaim (CreatedAt within the
            // window) is never removed. Then race to re-insert: success ⇒ we reclaimed it; failure ⇒ still in-flight.
            var staleMinutes = Math.Max(1, (int)staleAfter.TotalMinutes);
            await db.Database.ExecuteSqlRawAsync(
                @"DELETE FROM ""ServiceCatalog"".""IdempotencyReservations""
                  WHERE ""RequestType"" = {0} AND ""Key"" = {1} AND ""Status"" = 'InFlight'
                    AND ""CreatedAt"" < now() - make_interval(mins => {2})",
                new object[] { requestType, key, staleMinutes }, cancellationToken);

            db.ChangeTracker.Clear();
            return await TryInsertAsync(db, requestType, key, cancellationToken)
                ? new IdempotencyOutcome(IdempotencyState.Reserved, null)
                : new IdempotencyOutcome(IdempotencyState.InFlight, null);
        }

        public async Task CompleteAsync(string requestType, string key, string resultJson, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                @"UPDATE ""ServiceCatalog"".""IdempotencyReservations""
                  SET ""Status"" = 'Completed', ""ResultJson"" = {2}::jsonb, ""CompletedAt"" = now()
                  WHERE ""RequestType"" = {0} AND ""Key"" = {1}",
                new object[] { requestType, key, resultJson }, cancellationToken);
        }

        public async Task ReleaseAsync(string requestType, string key, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            // Only release an in-flight reservation — never delete a completed result.
            await db.Database.ExecuteSqlRawAsync(
                @"DELETE FROM ""ServiceCatalog"".""IdempotencyReservations""
                  WHERE ""RequestType"" = {0} AND ""Key"" = {1} AND ""Status"" = 'InFlight'",
                new object[] { requestType, key }, cancellationToken);
        }

        private static async Task<bool> TryInsertAsync(ServiceCatalogDbContext db, string requestType, string key, CancellationToken ct)
        {
            db.IdempotencyReservations.Add(new IdempotencyReservation
            {
                RequestType = requestType,
                Key = key,
                Status = IdempotencyReservation.InFlight,
                CreatedAt = DateTime.UtcNow
            });
            try
            {
                await db.SaveChangesAsync(ct);
                return true;
            }
            catch (DbUpdateException)
            {
                db.ChangeTracker.Clear(); // the row already exists — not our reservation
                return false;
            }
        }
    }
}
