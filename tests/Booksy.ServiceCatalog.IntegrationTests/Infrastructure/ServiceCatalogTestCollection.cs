namespace Booksy.ServiceCatalog.IntegrationTests.Infrastructure;

/// <summary>
/// One booted <see cref="ServiceCatalogTestWebApplicationFactory{TStartup}"/> — one host, one
/// database — shared by every class in this project, replacing the <c>IClassFixture&lt;TFactory&gt;</c>
/// on <c>IntegrationTestBase</c> that gave each of the 43 classes here its own boot.
///
/// <para>Safe now, and not before: docs/TEST_ARCHITECTURE_AUDIT.md Phase 2 slice 1 gave every test
/// a real reset (database, caches, capturing fakes) in <c>Factory.ResetStateAsync()</c>, so a class
/// no longer depends on starting from a factory nobody else has touched. Measured with the
/// per-class boots still in place: the median class boot was already down to 1.47 s (from an
/// original ~9 s) once Phase 1 quieted host logging — this collection removes the remaining 43
/// boots down to one, which is the rest of the ~75 s of class-boot time the last slice-1 FULL run
/// spent on this suite.</para>
///
/// <para>Joining the collection also serialises these classes against each other for now (xUnit
/// does not run test collections in parallel unless configured to), matching what
/// <c>[assembly: CollectionBehavior(DisableTestParallelization = true)]</c> in
/// <c>AssemblyInfo.cs</c> already enforced. Slice 5 revisits parallelism once the shared host is
/// stable.</para>
/// </summary>
[CollectionDefinition(Name)]
public sealed class ServiceCatalogTestCollection : ICollectionFixture<ServiceCatalogTestWebApplicationFactory<Startup>>
{
    public const string Name = "Booksy.ServiceCatalog";

    // No code. The class exists to carry [CollectionDefinition] and ICollectionFixture<>.
}
