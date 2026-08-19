using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Seeders
{
    /// <summary>
    /// Makes the seeded catalogue bookable: qualifies each provider's staff for that provider's services and then
    /// activates them.
    ///
    /// <para><see cref="ServiceSeeder"/> deliberately creates services in <see cref="ServiceStatus.Draft"/> — that is
    /// what <c>Service.Create</c> produces, and a draft service cannot be activated before somebody can perform it.
    /// The availability engine rejects a non-Active service outright ("این خدمت فعال نیست"), so a catalogue of
    /// drafts shows zero available times however good the business hours are.</para>
    ///
    /// <para>This runs through the domain, not around it: <c>AddQualifiedStaff</c> then <c>Activate</c>, mirroring
    /// exactly what <c>MemberBookabilityService</c> does when a real member joins an organization. The
    /// "an organization's service needs at least one qualified staff member" invariant is satisfied, never bypassed,
    /// which is why the seeder depends on <see cref="StaffSeeder"/> having run first.</para>
    ///
    /// <para>Idempotent and repairing: it only touches services that are not yet Active, skips staff already
    /// qualified, and leaves a provider's services in Draft if that provider genuinely has no member to perform
    /// them (rather than forcing a status the domain would refuse).</para>
    /// </summary>
    public sealed class ServiceActivationSeeder : ISeeder
    {
        private readonly ServiceCatalogDbContext _context;
        private readonly ILogger<ServiceActivationSeeder> _logger;

        public ServiceActivationSeeder(
            ServiceCatalogDbContext context,
            ILogger<ServiceActivationSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var pendingServices = await _context.Services
                    .Where(s => s.Status == ServiceStatus.Draft)
                    .ToListAsync(cancellationToken);

                if (pendingServices.Count == 0)
                {
                    _logger.LogInformation("No draft services to activate. Skipping...");
                    return;
                }

                // The bookable resource id IS the membership id — the same identity the availability engine and
                // Booking.StaffId use. Anything else here would qualify a staff member who can never be resolved.
                var membershipIdsByOrganization = (await _context.OrganizationMemberships
                        .Where(m => m.Status == MembershipStatus.Active)
                        .ToListAsync(cancellationToken))
                    .Where(m => m.ProvidesServices)
                    .GroupBy(m => m.OrganizationId.Value)
                    .ToDictionary(g => g.Key, g => g.Select(m => m.Id).ToList());

                if (membershipIdsByOrganization.Count == 0)
                {
                    _logger.LogWarning(
                        "No active service-providing members found; {Count} service(s) stay in Draft because no one can perform them.",
                        pendingServices.Count);
                    return;
                }

                var activated = 0;
                var qualifications = 0;
                var withoutStaff = 0;

                foreach (var service in pendingServices)
                {
                    if (!membershipIdsByOrganization.TryGetValue(service.ProviderId.Value, out var membershipIds)
                        || membershipIds.Count == 0)
                    {
                        withoutStaff++;
                        continue;
                    }

                    foreach (var membershipId in membershipIds)
                    {
                        if (service.IsStaffQualified(membershipId))
                            continue;

                        service.AddQualifiedStaff(membershipId);
                        qualifications++;
                    }

                    service.Activate();
                    activated++;
                }

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Activated {Activated} service(s) with {Qualifications} staff qualification(s); {WithoutStaff} left in Draft for lack of staff",
                    activated, qualifications, withoutStaff);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating seeded services");
                throw;
            }
        }
    }
}
