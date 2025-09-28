// ========================================
// Booksy.Tests.Common/Builders/ServiceBuilder.cs
// ========================================
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.Tests.Common.Builders;

/// <summary>
/// Builder for creating Service test data
/// Uses Fluent API for readable test setup
/// </summary>
public class ServiceBuilder
{
    private readonly Fixture _fixture;
    private ProviderId? _providerId;
    private string? _name;
    private string? _description;
    private ServiceCategory? _category;
    private ServiceType _serviceType = ServiceType.Standard;
    private Price? _price;
    private Duration? _duration;
    private ServiceStatus _status = ServiceStatus.Draft;

    public ServiceBuilder()
    {
        _fixture = new Fixture();
    }

    public ServiceBuilder WithProviderId(ProviderId providerId)
    {
        _providerId = providerId;
        return this;
    }

    public ServiceBuilder WithProvider(Guid providerId)
    {
        _providerId = ProviderId.Create(providerId);
        return this;
    }

    public ServiceBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ServiceBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public ServiceBuilder WithCategory(string categoryName)
    {
        _category = ServiceCategory.Create(categoryName);
        return this;
    }

    public ServiceBuilder WithType(ServiceType serviceType)
    {
        _serviceType = serviceType;
        return this;
    }

    public ServiceBuilder WithPrice(decimal amount, string currency = "USD")
    {
        _price = Price.Create(amount, currency);
        return this;
    }

    public ServiceBuilder WithDuration(int minutes)
    {
        _duration = Duration.FromMinutes(minutes);
        return this;
    }

    public ServiceBuilder WithStatus(ServiceStatus status)
    {
        _status = status;
        return this;
    }

    public ServiceBuilder AsActive()
    {
        _status = ServiceStatus.Active;
        return this;
    }

    public ServiceBuilder AsPremium()
    {
        _serviceType = ServiceType.Premium;
        return this;
    }

    public Service Build()
    {
        var service = Service.Create(
            _providerId ?? ProviderId.Create(Guid.NewGuid()),
            _name ?? _fixture.Create<string>(),
            _description ?? _fixture.Create<string>(),
            _category ?? ServiceCategory.Create("Test Category"),
            _serviceType,
            _price ?? Price.Create(50m, "USD"),
            _duration ?? Duration.FromMinutes(60)
        );

        // Set status if different from default
        if (_status != ServiceStatus.Draft)
        {
            // Use reflection or internal method to set status
            SetPrivateProperty(service, "Status", _status);
        }

        return service;
    }

    public List<Service> BuildMany(int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => Build())
            .ToList();
    }

    private static void SetPrivateProperty<T>(T entity, string propertyName, object value)
    {
        var property = typeof(T).GetProperty(propertyName,
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        property?.SetValue(entity, value);
    }
}




/// <summary>
/// Builder for creating Provider test data
/// </summary>
public class ProviderBuilder
{
    private readonly Fixture _fixture;
    private UserId? _ownerId;
    private string? _businessName;
    private string? _description;
    private ProviderType _providerType = ProviderType.Salon;
    private ContactInfo? _contactInfo;
    private BusinessAddress? _address;
    private ProviderStatus _status = ProviderStatus.Active;
    private bool _allowOnlineBooking = true;

    public ProviderBuilder()
    {
        _fixture = new Fixture();
    }

    public ProviderBuilder WithOwnerId(UserId ownerId)
    {
        _ownerId = ownerId;
        return this;
    }

    public ProviderBuilder WithOwner(Guid ownerId)
    {
        _ownerId = UserId.From(ownerId);
        return this;
    }

    public ProviderBuilder WithBusinessName(string businessName)
    {
        _businessName = businessName;
        return this;
    }

    public ProviderBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public ProviderBuilder WithType(ProviderType providerType)
    {
        _providerType = providerType;
        return this;
    }

    public ProviderBuilder WithEmail(string email)
    {
        _contactInfo = ContactInfo.Create(
            Email.Create(email),
            PhoneNumber.Create("+1234567890")
        );
        return this;
    }

    public ProviderBuilder WithStatus(ProviderStatus status)
    {
        _status = status;
        return this;
    }

    public ProviderBuilder AsActive()
    {
        _status = ProviderStatus.Active;
        return this;
    }

    public ProviderBuilder AsInactive()
    {
        _status = ProviderStatus.Inactive;
        return this;
    }

    public ProviderBuilder WithOnlineBooking(bool allowed)
    {
        _allowOnlineBooking = allowed;
        return this;
    }

    public Provider Build()
    {
        var provider = Provider.RegisterProvider(
            _ownerId ?? UserId.From(Guid.NewGuid()),
            _businessName ?? _fixture.Create<string>(),
            _description ?? _fixture.Create<string>(),
            _providerType,
            _contactInfo ?? ContactInfo.Create(
                Email.Create("test@example.com"),
                PhoneNumber.Create("+1234567890")
            ),
            _address ?? BusinessAddress.Create(
                "123 Test St",
                "Test City",
                "TS",
                "12345",
                "USA"
            )
        );

        provider.SetSatus(_status);
        provider.SetAllowOnlineBooking(_allowOnlineBooking);

        return provider;
    }

    public List<Provider> BuildMany(int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => Build())
            .ToList();
    }
}



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