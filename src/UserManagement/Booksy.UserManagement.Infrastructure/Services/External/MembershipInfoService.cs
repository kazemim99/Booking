using Booksy.UserManagement.Application.Services.Interfaces;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Booksy.UserManagement.Infrastructure.Services.External;

/// <summary>
/// INTEGRATION SEAM (read-only): resolves a person's organization memberships from the
/// ServiceCatalog schema of the shared monolith database, so an issued JWT can carry them.
/// Mirrors ServiceCatalog's <c>PersonDirectoryReadService</c> — the reverse direction of that same
/// seam: a single, clearly-marked file, plain SQL over committed data on the shared connection, no
/// domain coupling to ServiceCatalog's types. If the contexts ever split databases this becomes a
/// real query API / event-fed read model.
///
/// Deliberately NOT implemented as an HTTP self-call (unlike the older <see cref="IProviderInfoService"/>
/// seam it sits beside) — this is one composed host process with one shared database, so a network
/// round-trip back into itself only adds latency and a new failure mode with no isolation benefit.
/// </summary>
public sealed class MembershipInfoService : IMembershipInfoService
{
    private const string Sql = """
        SELECT id, organization_id, roles, status
        FROM "ServiceCatalog".organization_memberships
        WHERE person_id = @personId AND status <> 'Terminated'
        """;

    private readonly UserManagementDbContext _context;

    public MembershipInfoService(UserManagementDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<MembershipSummary>> GetMembershipsForPersonAsync(
        Guid personId, CancellationToken cancellationToken = default)
    {
        var result = new List<MembershipSummary>();

        var connection = _context.Database.GetDbConnection();
        var wasOpen = connection.State == System.Data.ConnectionState.Open;
        if (!wasOpen)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = Sql;
            command.Parameters.Add(new NpgsqlParameter("personId", personId));

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new MembershipSummary(
                    MembershipId: reader.GetGuid(0),
                    OrganizationId: reader.GetGuid(1),
                    Roles: reader.GetString(2),
                    Status: reader.GetString(3)));
            }

            return result;
        }
        finally
        {
            if (!wasOpen)
                await connection.CloseAsync();
        }
    }
}
