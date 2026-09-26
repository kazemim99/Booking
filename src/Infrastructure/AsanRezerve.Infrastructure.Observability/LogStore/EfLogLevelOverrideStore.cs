using AsanRezerve.Infrastructure.Observability.Logging.Levels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.Infrastructure.Observability.LogStore;

/// <summary>Log-level overrides in <c>observability.log_level_overrides</c>, so they survive a restart.</summary>
public sealed class EfLogLevelOverrideStore(IServiceScopeFactory scopes) : ILogLevelOverrideStore
{
    public async Task<IReadOnlyList<LogLevelOverride>> LoadAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ObservabilityDbContext>();
        var rows = await db.LogLevelOverrides.AsNoTracking().ToListAsync(cancellationToken);
        return rows
            .Where(r => Enum.TryParse<LogLevel>(r.Level, out _))
            .Select(r => new LogLevelOverride(r.Category, Enum.Parse<LogLevel>(r.Level), r.ExpiresAt, r.UpdatedBy, r.UpdatedAt))
            .ToList();
    }

    public async Task UpsertAsync(LogLevelOverride value, CancellationToken cancellationToken = default)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ObservabilityDbContext>();
        var row = await db.LogLevelOverrides.FindAsync([value.Category], cancellationToken);
        if (row is null)
        {
            row = new LogLevelOverrideRecord { Category = value.Category };
            db.LogLevelOverrides.Add(row);
        }

        row.Level = value.Level.ToString();
        row.ExpiresAt = value.ExpiresAt?.ToUniversalTime();
        row.UpdatedBy = value.UpdatedBy;
        row.UpdatedAt = value.UpdatedAt.ToUniversalTime();
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string category, CancellationToken cancellationToken = default)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ObservabilityDbContext>();
        await db.LogLevelOverrides.Where(r => r.Category == category).ExecuteDeleteAsync(cancellationToken);
    }
}
