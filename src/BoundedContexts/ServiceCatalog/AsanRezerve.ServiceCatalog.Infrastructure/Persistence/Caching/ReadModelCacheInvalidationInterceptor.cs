using AsanRezerve.Core.Application.Abstractions.Caching;
using AsanRezerve.ServiceCatalog.Application.Caching;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Caching;

/// <summary>
/// Evicts cached salon reads whenever anything about a salon is saved (add-observability-and-caching, D8).
/// <para>Invalidation used to be driven by a hand-picked list of domain events. That missed every mutator that
/// raises none (a service's price or duration, a staff member leaving, a gallery caption), every change committed
/// straight through the DbContext, and it ran before <c>SaveChanges</c>. Here the change tracker is the source of
/// truth: before the save, every added/modified/deleted row is traced to the salon it belongs to — the provider
/// itself and its owned parts (profile, gallery, address, policy), its services and their owned parts, its staff
/// memberships, and any row with a foreign key to the provider (opening hours, holidays, exceptions). After a
/// successful save their tags are evicted; <see cref="ICacheInvalidator"/> evicts them once more when the scope
/// ends, after the transaction committed.</para>
/// </summary>
public sealed class ReadModelCacheInvalidationInterceptor : SaveChangesInterceptor
{
    private readonly ICacheInvalidator _invalidator;
    private readonly HashSet<Guid> _pending = [];

    public ReadModelCacheInvalidationInterceptor(ICacheInvalidator invalidator)
    {
        _invalidator = invalidator;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Collect(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Collect(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        FlushAsync(CancellationToken.None).GetAwaiter().GetResult();
        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        await FlushAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData) => _pending.Clear();

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        _pending.Clear();
        return Task.CompletedTask;
    }

    private async Task FlushAsync(CancellationToken cancellationToken)
    {
        if (_pending.Count == 0) return;

        var tags = _pending.SelectMany(ReadModelCacheTags.ForProviderChange).Distinct().ToArray();
        _pending.Clear();
        await _invalidator.InvalidateAsync(tags, cancellationToken).ConfigureAwait(false);
    }

    private void Collect(DbContext? context)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;

            if (SalonOf(context, entry) is Guid providerId)
                _pending.Add(providerId);
        }
    }

    /// <summary>The salon a changed row belongs to, or null when it belongs to none.</summary>
    private static Guid? SalonOf(DbContext context, EntityEntry entry)
    {
        var root = entry.Metadata.IsOwned() ? OwnerOf(context, entry) : entry;

        return root?.Entity switch
        {
            null => null,
            Provider provider => provider.Id?.Value,
            Service service => service.ProviderId?.Value,
            OrganizationMembership membership => membership.OrganizationId?.Value,
            _ => ProviderForeignKey(root),
        };
    }

    /// <summary>
    /// The aggregate an owned row belongs to. An owned type's key starts with its owner's key, all the way up the
    /// ownership chain (a gallery image → the profile → the provider), so the first ownership-key value of the row
    /// is the root's key.
    /// </summary>
    private static EntityEntry? OwnerOf(DbContext context, EntityEntry entry)
    {
        var ownership = entry.Metadata.FindOwnership()!;
        var property = entry.Property(ownership.Properties[0].Name);
        var rootKey = property.CurrentValue ?? property.OriginalValue;
        if (rootKey is null) return null;

        IReadOnlyEntityType rootType = entry.Metadata;
        while (rootType.IsOwned())
            rootType = rootType.FindOwnership()!.PrincipalEntityType;

        return context.ChangeTracker.Entries().FirstOrDefault(candidate =>
            candidate.Metadata == rootType
            && Equals(candidate.Property(rootType.FindPrimaryKey()!.Properties[0].Name).CurrentValue, rootKey));
    }

    /// <summary>A row that references a provider by foreign key: opening hours, holidays, exceptions.</summary>
    private static Guid? ProviderForeignKey(EntityEntry entry)
    {
        var foreignKey = entry.Metadata.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Provider));
        if (foreignKey is null) return null;

        var property = entry.Property(foreignKey.Properties[0].Name);
        return (property.CurrentValue ?? property.OriginalValue) switch
        {
            ProviderId id => id.Value,
            Guid id => id,
            _ => null,
        };
    }
}
