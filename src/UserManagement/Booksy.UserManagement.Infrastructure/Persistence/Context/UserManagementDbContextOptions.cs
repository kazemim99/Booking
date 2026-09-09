using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Booksy.UserManagement.Infrastructure.Persistence.Context
{
    /// <summary>
    /// The one place the UserManagement <see cref="DbContext"/> options are defined. Production DI
    /// and the integration tests both go through here, so a test-built context has exactly the
    /// provider settings, migrations assembly, history table and warning configuration of the
    /// real one — rather than a hand-rolled <c>UseNpgsql(connectionString)</c> that silently
    /// differs (which is how <c>UserRepositorySaveTests</c> came to fail on EF's
    /// pending-model-changes check while the application itself did not).
    /// </summary>
    public static class UserManagementDbContextOptions
    {
        public const string Schema = "user_management";
        public const string MigrationsHistoryTable = "__EFMigrationsHistory";

        /// <summary>
        /// Applies the UserManagement provider configuration to <paramref name="options"/>.
        /// </summary>
        public static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, string connectionString)
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(UserManagementDbContext).Assembly.FullName);
                npgsqlOptions.MigrationsHistoryTable(MigrationsHistoryTable, Schema);
                npgsqlOptions.CommandTimeout(30);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });

            // EF Core 9 raises PendingModelChangesWarning on Migrate() when the model hash differs
            // from the last snapshot. The application has always suppressed it here; whether the
            // model genuinely has pending changes is tracked separately (see the change log for
            // reduce-integration-baseline-um-payments), and until that is settled every context —
            // production or test — must behave the same way, or tests fail for a reason the
            // application never sees.
            options.ConfigureWarnings(warnings =>
                warnings.Ignore(RelationalEventId.PendingModelChangesWarning));

            return options;
        }
    }
}
