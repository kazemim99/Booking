// ========================================
// Booksy.Tests.Common/Builders/ServiceBuilder.cs
// ========================================
using AutoFixture;
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
        _providerId = ProviderId.From(providerId);
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
        _category = ServiceCategory.HairSalon;
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
            _providerId ?? ProviderId.From(Guid.NewGuid()),
            _name ?? _fixture.Create<string>(),
            _description ?? _fixture.Create<string>(),
            _category ?? ServiceCategory.HairSalon,
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
