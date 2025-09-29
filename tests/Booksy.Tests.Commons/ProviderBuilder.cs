// ========================================
// Booksy.Tests.Common/Builders/ServiceBuilder.cs
// ========================================
using AutoFixture;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.Tests.Common.Builders;

/// <summary>
/// Builder for creating Provider test data
/// </summary>
public class ProviderBuilder
{
    private readonly Fixture _fixture;
    private UserId? _ownerId;
    private string? _businessName;
    private string? _description;
    private BusinessSize _providerType = BusinessSize.Individual;
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

    public ProviderBuilder WithType(BusinessSize providerType)
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
