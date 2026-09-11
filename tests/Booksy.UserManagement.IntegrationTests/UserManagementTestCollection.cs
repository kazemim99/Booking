namespace Booksy.UserManagement.IntegrationTests;

/// <summary>
/// One booted <see cref="UserManagementTestWebApplicationFactory{TStartup}"/> — one Booksy.Host,
/// one database — shared by every class in this project, replacing the per-class
/// <c>IClassFixture&lt;TFactory&gt;</c> that <see cref="UserManagementIntegrationTestBase"/> declared
/// while this suite still booted the retired per-service host (docs/TEST_ARCHITECTURE_AUDIT.md
/// Phase 2 slice 3). Mirrors <c>ServiceCatalogTestCollection</c> (slice 2); safe for the same
/// reason: every test resets the database, the caches and every capturing fake before it runs
/// (slice 1), so a class no longer depends on starting from a factory nobody else has touched.
///
/// <para>The two raw-<c>DbContext</c> classes (<c>UserRepositorySaveTests</c>,
/// <c>PersonProvisioningConcurrencyTests</c>) join this collection too, taking their
/// <c>UserManagementDbContext</c> from the SAME factory's DI scope instead of hand-building one
/// with substituted services against their own throwaway container — see their own remarks for
/// what that fixes.</para>
/// </summary>
[CollectionDefinition(Name)]
public sealed class UserManagementTestCollection : ICollectionFixture<UserManagementTestWebApplicationFactory<Startup>>
{
    public const string Name = "Booksy.UserManagement";

    // No code. The class exists to carry [CollectionDefinition] and ICollectionFixture<>.
}
