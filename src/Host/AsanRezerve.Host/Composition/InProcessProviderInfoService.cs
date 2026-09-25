using AsanRezerve.ServiceCatalog.Application.Abstractions;
using AsanRezerve.ServiceCatalog.Application.Queries.Provider.GetProviderByOwnerId;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using AsanRezerve.UserManagement.Application.Services.Interfaces;
using MediatR;

namespace AsanRezerve.Host.Composition;

/// <summary>
/// In-process implementation of UserManagement's <see cref="IProviderInfoService"/> port,
/// answering "which provider does this person own?" by dispatching ServiceCatalog's
/// <see cref="GetProviderByOwnerIdQuery"/> directly instead of calling its HTTP endpoint.
///
/// <para><b>Why this lives in the Host.</b> This is the composition root — the only project
/// that references both bounded contexts. UserManagement.Infrastructure deliberately has no
/// ServiceCatalog reference, so its HTTP adapter
/// (<c>UserManagement.Infrastructure.Services.External.ProviderInfoService</c>) was the only
/// way it could reach ServiceCatalog. Implementing the same port here keeps the contexts
/// free of a compile-time dependency on each other while removing the loopback call.</para>
///
/// <para><b>Why the HTTP adapter could not work at all on this path.</b> It called
/// <c>GET /api/v1/Providers/by-owner/{ownerId}</c>, which carries no <c>[AllowAnonymous]</c>
/// and therefore falls under the host-wide <c>FallbackPolicy.RequireAuthenticatedUser()</c>.
/// Its only caller is OTP sign-in completion — where the caller is, by definition, not yet
/// authenticated and has no bearer token to forward — so the request always 401'd. The
/// exception was caught and logged as a warning, so the failure was silent: every sign-in
/// reported no provider profile, dropping <c>providerId</c>/<c>providerStatus</c> from the
/// issued token and sending fully-registered providers back into onboarding.</para>
///
/// <para>Running in-process also removes a serialization round-trip and makes the lookup
/// share the caller's transaction and DbContext scope.</para>
/// </summary>
/// <remarks>
/// Public rather than internal so AsanRezerve.Host.CompositionTests can assert the container
/// resolves this exact type (xUnit requires public test classes, which cannot consume a
/// fixture whose base type is closed over an internal type). AsanRezerve.Host is an application
/// assembly, never packaged as a library, so this widens no consumable API surface.
/// </remarks>
public sealed class InProcessProviderInfoService : IProviderInfoService
{
    private readonly ISender _mediator;
    private readonly IProviderReadRepository _providers;
    private readonly IUrlService _urls;

    public InProcessProviderInfoService(ISender mediator, IProviderReadRepository providers, IUrlService urls)
    {
        _mediator = mediator;
        _providers = providers;
        _urls = urls;
    }

    public async Task<ProviderInfo?> GetProviderByOwnerIdAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var provider = await _mediator.Send(new GetProviderByOwnerIdQuery(ownerId), cancellationToken);

        // A person with no provider profile is an expected outcome (a customer, or a
        // provider who has not started registration), not an error — the handler returns
        // null and the caller treats it as "needs onboarding".
        return provider is null
            ? null
            : new ProviderInfo(provider.Id, provider.Status.ToString());
    }

    /// <summary>
    /// A customer's favourites and recent visits store only salon ids; this answers what their cards show, in ONE
    /// query (customer-app-ux-review-fixes). Only Active salons come back — one that was archived, suspended or never
    /// approved since the customer saved or opened it is left out rather than sent as a row the app cannot open.
    /// </summary>
    public async Task<IReadOnlyDictionary<Guid, SalonCard>> GetActiveSalonCardsAsync(
        IReadOnlyCollection<Guid> providerIds,
        CancellationToken cancellationToken = default)
    {
        var ids = providerIds.Distinct().Select(ProviderId.From).ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, SalonCard>();

        var salons = await _providers.GetAsync(new ActiveSalonsByIdsSpecification(ids), cancellationToken);

        return salons.ToDictionary(
            p => p.Id.Value,
            p => new SalonCard(
                p.Profile.BusinessName,
                _urls.AbsoluteOrNull(p.Profile.DisplayImageUrl),
                p.Address?.City,
                p.AverageRating,
                p.PublishedReviewCount));
    }

    private sealed class ActiveSalonsByIdsSpecification : BaseSpecification<Provider>
    {
        public ActiveSalonsByIdsSpecification(List<ProviderId> ids)
            : base(p => ids.Contains(p.Id) && p.Status == ProviderStatus.Active)
        {
        }
    }
}
