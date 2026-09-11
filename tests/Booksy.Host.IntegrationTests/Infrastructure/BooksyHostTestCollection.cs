namespace Booksy.Host.IntegrationTests.Infrastructure;

/// <summary>
/// One booted <see cref="BooksyHostFactory"/> — one host, one database — shared by every
/// ServiceCatalog and UserManagement test class in this project (docs/TEST_ARCHITECTURE_AUDIT.md
/// Phase 2 slice 4). Replaces the two separate collections each suite had while they still booted
/// through two different closed generic factory types
/// (<c>ServiceCatalogTestCollection</c>, <c>UserManagementTestCollection</c>) — merging the factory
/// into one non-generic <see cref="BooksyHostFactory"/> is what makes ONE collection for both
/// possible, taking the suite from 43 (pre-Phase-2) host boots down to one.
///
/// <para>Composition tests do not join this collection: they need the real, unfaked production DI
/// graph (<see cref="Booksy.Host.IntegrationTests.Composition.HostCompositionFactory"/>), which this
/// factory's fakes would defeat the purpose of asserting on.</para>
/// </summary>
[CollectionDefinition(Name)]
public sealed class BooksyHostTestCollection : ICollectionFixture<BooksyHostFactory>
{
    public const string Name = "Booksy.Host";

    // No code. The class exists to carry [CollectionDefinition] and ICollectionFixture<>.
}
