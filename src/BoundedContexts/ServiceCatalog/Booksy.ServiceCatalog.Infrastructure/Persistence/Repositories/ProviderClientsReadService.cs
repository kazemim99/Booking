// ========================================
// Booksy.ServiceCatalog.Infrastructure/Persistence/Repositories/ProviderClientsReadService.cs
// ========================================
using Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderClients;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// INTEGRATION SEAM (read-only): derives the provider's client book by
    /// aggregating ServiceCatalog bookings per customer and resolving names/
    /// phones from the UserManagement schema of the shared monolith database
    /// (user_profiles.user_id = users.id, verified live 2026-07-16).
    ///
    /// This is deliberately a single, clearly-marked file: if the contexts
    /// ever split databases, this becomes an event-fed read model. No domain
    /// coupling — plain SQL over committed data.
    ///
    /// The provider's OWN user is excluded: walk-in bookings are created
    /// under it (composer MVP convention) and would otherwise aggregate into
    /// a meaningless "client".
    /// </summary>
    public sealed class ProviderClientsReadService : IProviderClientsReadService
    {
        private const string Sql = """
            SELECT
                b."CustomerId"                                   AS customer_id,
                COALESCE(NULLIF(TRIM(CONCAT(p.first_name, ' ', p.last_name)), ''), '') AS name,
                COALESCE(u."PhoneNumber", '')                    AS phone,
                COUNT(*)::int                                    AS total,
                COUNT(*) FILTER (WHERE b."Status" = 'Completed')::int AS completed,
                COUNT(*) FILTER (
                    WHERE b."StartTime" >= NOW()
                      AND b."Status" IN ('Requested', 'Confirmed'))::int AS upcoming,
                MAX(b."StartTime") FILTER (WHERE b."StartTime" < NOW()) AS last_visit
            FROM "ServiceCatalog"."Bookings" b
            LEFT JOIN user_management.users u          ON u.id = b."CustomerId"
            LEFT JOIN user_management.user_profiles p  ON p.user_id = b."CustomerId"
            WHERE b."ProviderId" = @providerId
              AND b."IsDeleted" = FALSE
              AND b."CustomerId" <> COALESCE(
                    (SELECT pr."OwnerId" FROM "ServiceCatalog"."Providers" pr
                     WHERE pr."Id" = @providerId),
                    '00000000-0000-0000-0000-000000000000')
            GROUP BY b."CustomerId", p.first_name, p.last_name, u."PhoneNumber"
            ORDER BY MAX(b."StartTime") DESC
            """;

        private readonly ServiceCatalogDbContext _context;

        public ProviderClientsReadService(ServiceCatalogDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<ProviderClientDto>> GetClientsAsync(
            Guid providerId,
            CancellationToken cancellationToken = default)
        {
            var connection = _context.Database.GetDbConnection();
            var wasOpen = connection.State == System.Data.ConnectionState.Open;
            if (!wasOpen)
            {
                await connection.OpenAsync(cancellationToken);
            }

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = Sql;
                command.Parameters.Add(
                    new NpgsqlParameter("providerId", providerId));

                var clients = new List<ProviderClientDto>();
                await using var reader =
                    await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    clients.Add(new ProviderClientDto(
                        CustomerId: reader.GetGuid(0),
                        Name: reader.GetString(1),
                        Phone: reader.GetString(2),
                        TotalBookings: reader.GetInt32(3),
                        CompletedBookings: reader.GetInt32(4),
                        UpcomingBookings: reader.GetInt32(5),
                        LastVisitAt: reader.IsDBNull(6)
                            ? null
                            : reader.GetDateTime(6)));
                }

                return clients;
            }
            finally
            {
                if (!wasOpen)
                {
                    await connection.CloseAsync();
                }
            }
        }
    }
}
