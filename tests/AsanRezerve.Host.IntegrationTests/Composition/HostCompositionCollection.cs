namespace AsanRezerve.Host.CompositionTests;

/// <summary>
/// One booted <see cref="HostCompositionFactory"/> — one PostgreSQL database, one migration of both
/// contexts, one composed service graph — shared by every class in this project.
///
/// <para>These classes used to declare <c>IClassFixture&lt;HostCompositionFactory&gt;</c>, which builds the
/// fixture <b>per class</b>: four boots of the whole monolith to answer questions about how services are
/// registered. Measured at 45 s for 21 tests, of which about 36 s was booting. Nothing here writes state
/// another class reads — the tests that do touch the database create their own rows with unique phone
/// numbers — so a single shared host is honest as well as faster.</para>
///
/// <para>Joining a collection also serialises these classes against each other, which they already were:
/// the alternative was four hosts competing for the same connection pool and CPU.</para>
/// </summary>
[CollectionDefinition(Name)]
public sealed class HostCompositionCollection : ICollectionFixture<HostCompositionFactory>
{
    public const string Name = "AsanRezerve.Host composition";

    // No code. The class exists to carry [CollectionDefinition] and ICollectionFixture<>.
}
