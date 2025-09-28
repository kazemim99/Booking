// ========================================
// Booksy.Tests.Common/Builders/ServiceBuilder.cs
// ========================================
using AutoFixture;
using AutoFixture.AutoNSubstitute;

namespace Booksy.Tests.Common.Builders;



/// <summary>
/// Custom AutoFixture configurations for test data generation
/// </summary>
public static class AutoFixtureCustomizations
{
    /// <summary>
    /// Creates a fixture with common customizations for domain testing
    /// </summary>
    public static IFixture CreateDomainFixture()
    {
        var fixture = new Fixture();

        // Use AutoNSubstitute for automatic mocking
        fixture.Customize(new AutoNSubstituteCustomization
        {
            ConfigureMembers = true
        });

        // Add custom value object generators
        // fixture.Customize<Email>(c => c.FromFactory(() => Email.Create("test@example.com")));
        // fixture.Customize<PhoneNumber>(c => c.FromFactory(() => PhoneNumber.Create("+1234567890")));

        return fixture;
    }

    /// <summary>
    /// Creates a fixture with API request customizations
    /// </summary>
    public static IFixture CreateApiFixture()
    {
        var fixture = CreateDomainFixture();

        // Add API-specific customizations
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => fixture.Behaviors.Remove(b));

        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        return fixture;
    }
}