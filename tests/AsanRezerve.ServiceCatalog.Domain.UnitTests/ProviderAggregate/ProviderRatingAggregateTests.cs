using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.UnitTests.ProviderAggregate;

/// <summary>
/// The provider's overall rating and published-review count, written together and only together.
/// </summary>
/// <remarks>
/// Before this, <c>AverageRating</c> was <c>internal set</c> with no mutator and no assignment anywhere, so every
/// provider read 0.0. A single method setting both values is what makes "never read one without the other"
/// mechanical: the count is how an unrated provider is told apart from a zero.
/// </remarks>
public class ProviderRatingAggregateTests
{
    private static Provider NewProvider() => Provider.RegisterProvider(
        UserId.From(Guid.NewGuid()),
        "سالن نمونه",
        "Description",
        ServiceCategory.HairSalon,
        ContactInfo.Create(Email.Create("owner@salon.example"), PhoneNumber.From("+989120000000")),
        BusinessAddress.Create("خیابان ولیعصر، پلاک ۱۲", "خیابان ولیعصر", "تهران", "تهران", "1234567890", "IR"));

    [Fact]
    public void A_new_provider_is_unrated()
    {
        var provider = NewProvider();

        Assert.Equal(0, provider.PublishedReviewCount);
        Assert.False(provider.HasRating);
    }

    [Fact]
    public void Setting_the_aggregates_records_both_values()
    {
        var provider = NewProvider();

        provider.SetRatingAggregates(4.25m, 4);

        Assert.Equal(4.25m, provider.AverageRating);
        Assert.Equal(4, provider.PublishedReviewCount);
        Assert.True(provider.HasRating);
    }

    [Fact]
    public void Dropping_to_no_published_reviews_makes_the_provider_unrated_again()
    {
        var provider = NewProvider();
        provider.SetRatingAggregates(4.0m, 1);

        provider.SetRatingAggregates(0m, 0);

        Assert.False(provider.HasRating);
        Assert.Equal(0m, provider.AverageRating);
    }

    [Fact]
    public void An_unrated_provider_cannot_carry_an_average()
    {
        // A stored average with no reviews behind it is exactly the lie this change removes.
        var provider = NewProvider();

        Assert.Throws<DomainValidationException>(() => provider.SetRatingAggregates(3.5m, 0));
    }

    [Theory]
    [InlineData(0.99, 1)]
    [InlineData(5.01, 3)]
    public void A_rated_provider_must_have_an_average_within_the_star_range(double average, int count)
    {
        var provider = NewProvider();

        Assert.Throws<DomainValidationException>(() => provider.SetRatingAggregates((decimal)average, count));
    }

    [Fact]
    public void The_count_cannot_be_negative()
    {
        var provider = NewProvider();

        Assert.Throws<DomainValidationException>(() => provider.SetRatingAggregates(0m, -1));
    }
}
