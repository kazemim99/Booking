using Booksy.UserManagement.Application.Services.Interfaces;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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

        // Every caller treats this lookup as best-effort (catch, log, continue without
        // membership claims). That only holds if a failure here cannot poison the caller's
        // transaction: this runs on the context's own connection, so when the pipeline has a
        // transaction open (the UserManagement-only host does; the composed host wraps the
        // ServiceCatalog context instead) a failed statement leaves Postgres in the "current
        // transaction is aborted" state and the caller's later COMMIT silently becomes a
        // ROLLBACK — the login succeeded from the client's point of view, but its refresh token
        // and session were never written. A savepoint confines the failure to this read.
        const string savepoint = "membership_lookup";
        var transaction = _context.Database.CurrentTransaction;
        if (transaction is not null)
            await transaction.CreateSavepointAsync(savepoint, cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = Sql;
            command.Transaction = transaction?.GetDbTransaction();
            command.Parameters.Add(new NpgsqlParameter("personId", personId));

            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    result.Add(new MembershipSummary(
                        MembershipId: reader.GetGuid(0),
                        OrganizationId: reader.GetGuid(1),
                        Roles: reader.GetString(2),
                        Status: reader.GetString(3)));
                }
            }

            if (transaction is not null)
                await transaction.ReleaseSavepointAsync(savepoint, cancellationToken);

            return result;
        }
        catch when (transaction is not null)
        {
            await transaction.RollbackToSavepointAsync(savepoint, cancellationToken);
            throw;
        }
        finally
        {
            if (!wasOpen)
                await connection.CloseAsync();
        }
    }
}
