// ========================================
// Booksy.ServiceCatalog.Infrastructure/Persistence/Repositories/PersonDirectoryReadService.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Abstractions.Identity;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// INTEGRATION SEAM (read-only): resolves a person from the UserManagement schema
    /// of the shared monolith database so the invitation flow can reuse an existing
    /// account by phone. Mirrors <see cref="ProviderClientsReadService"/> — a single,
    /// clearly-marked file, plain SQL over committed data, no domain coupling. If the
    /// contexts ever split databases this becomes a query API / event-fed read model.
    ///
    /// Phone is matched against both the canonical stored value and the national number,
    /// because historically stored phone values are not guaranteed canonical.
    /// Soft-deleted accounts (Status = 'Deleted') are excluded.
    /// </summary>
    public sealed class PersonDirectoryReadService : IPersonDirectory
    {
        private const string Sql = """
            SELECT u.id, p.first_name, p.last_name, u."PhoneNumber", u."Status"
            FROM user_management.users u
            LEFT JOIN user_management.user_profiles p ON p.user_id = u.id
            WHERE (u."PhoneNumber" = @canonical OR u."NationalNumber" = @national)
              AND u."Status" <> 'Deleted'
            LIMIT 1
            """;

        private const string SqlByIds = """
            SELECT u.id, p.first_name, p.last_name, u."PhoneNumber", u."Status"
            FROM user_management.users u
            LEFT JOIN user_management.user_profiles p ON p.user_id = u.id
            WHERE u.id = ANY(@ids) AND u."Status" <> 'Deleted'
            """;

        private readonly ServiceCatalogDbContext _context;

        public PersonDirectoryReadService(ServiceCatalogDbContext context)
        {
            _context = context;
        }

        public async Task<PersonInfo?> FindByPhoneAsync(string phone, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return null;

            // Normalize once so we can match either stored form.
            string canonical;
            string national;
            try
            {
                var vo = PhoneNumber.From(phone);
                canonical = vo.Value;
                national = vo.NationalNumber;
            }
            catch
            {
                // Not a parseable phone — nothing can match.
                return null;
            }

            var connection = _context.Database.GetDbConnection();
            var wasOpen = connection.State == System.Data.ConnectionState.Open;
            if (!wasOpen)
                await connection.OpenAsync(cancellationToken);

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = Sql;
                command.Parameters.Add(new NpgsqlParameter("canonical", canonical));
                command.Parameters.Add(new NpgsqlParameter("national", national));

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                    return null;

                return new PersonInfo(
                    PersonId: reader.GetGuid(0),
                    FirstName: reader.IsDBNull(1) ? null : reader.GetString(1),
                    LastName: reader.IsDBNull(2) ? null : reader.GetString(2),
                    PhoneNumber: reader.IsDBNull(3) ? null : reader.GetString(3),
                    Status: reader.IsDBNull(4) ? "Unknown" : reader.GetString(4));
            }
            finally
            {
                if (!wasOpen)
                    await connection.CloseAsync();
            }
        }

        public async Task<IReadOnlyDictionary<Guid, PersonInfo>> FindByIdsAsync(
            IReadOnlyCollection<Guid> personIds,
            CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<Guid, PersonInfo>();
            if (personIds is null || personIds.Count == 0)
                return result;

            var connection = _context.Database.GetDbConnection();
            var wasOpen = connection.State == System.Data.ConnectionState.Open;
            if (!wasOpen)
                await connection.OpenAsync(cancellationToken);

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = SqlByIds;
                command.Parameters.Add(new NpgsqlParameter("ids", personIds.ToArray()));

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var id = reader.GetGuid(0);
                    result[id] = new PersonInfo(
                        PersonId: id,
                        FirstName: reader.IsDBNull(1) ? null : reader.GetString(1),
                        LastName: reader.IsDBNull(2) ? null : reader.GetString(2),
                        PhoneNumber: reader.IsDBNull(3) ? null : reader.GetString(3),
                        Status: reader.IsDBNull(4) ? "Unknown" : reader.GetString(4));
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
}
