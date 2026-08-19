using Booksy.ServiceCatalog.Application.Common;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Enums.Extensions;
using FluentAssertions;

namespace Booksy.ServiceCatalog.Application.UnitTests.Common;

/// <summary>
/// Unit tests for <see cref="ServiceCategoryResolver"/>, the single parser every registration
/// path uses to turn a client-supplied category string into a <see cref="ServiceCategory"/>.
///
/// <para>It replaced three disagreeing implementations. The regressions worth naming:</para>
/// <list type="bullet">
/// <item><description>The wizard sends <c>barber</c> for a men's barbershop. No handler's map
/// listed it, so every barbershop registered as <c>BeautySalon</c>.</description></item>
/// <item><description>A bare <c>Enum.TryParse</c> accepts arbitrary numbers, so a payload of
/// <c>"0"</c> or <c>"99"</c> produced an undefined category with no display metadata.</description></item>
/// </list>
/// </summary>
public class ServiceCategoryResolverTests
{
    #region The barbershop regression

    [Theory]
    [InlineData("barber")]
    [InlineData("Barber")]
    [InlineData("barbershop")]
    public void A_mens_barbershop_resolves_to_Barbershop(string categoryId)
    {
        ServiceCategoryResolver.TryResolve(categoryId, out var category).Should().BeTrue();
        category.Should().Be(ServiceCategory.Barbershop);
    }

    [Fact]
    public void A_mens_barbershop_is_never_silently_defaulted_to_BeautySalon()
    {
        ServiceCategoryResolver
            .Resolve("barber", fallback: ServiceCategory.BeautySalon)
            .Should().Be(ServiceCategory.Barbershop);
    }

    #endregion

    #region Accepted input shapes

    [Theory]
    [InlineData("hair_salon", ServiceCategory.HairSalon)]
    [InlineData("hair-salon", ServiceCategory.HairSalon)]
    [InlineData("nail_salon", ServiceCategory.NailSalon)]
    [InlineData("massage", ServiceCategory.Massage)]
    [InlineData("home_services", ServiceCategory.HomeServices)]
    public void Wizard_slugs_resolve(string input, ServiceCategory expected)
    {
        ServiceCategoryResolver.TryResolve(input, out var category).Should().BeTrue();
        category.Should().Be(expected);
    }

    [Theory]
    [InlineData("HairSalon", ServiceCategory.HairSalon)]
    [InlineData("hairSalon", ServiceCategory.HairSalon)]
    [InlineData("MEDICALCLINIC", ServiceCategory.MedicalClinic)]
    public void Enum_member_names_resolve_in_any_casing(string input, ServiceCategory expected)
    {
        // "hairSalon" is what the API itself emits — JsonStringEnumConverter with a camelCase policy.
        ServiceCategoryResolver.TryResolve(input, out var category).Should().BeTrue();
        category.Should().Be(expected);
    }

    [Theory]
    [InlineData("1", ServiceCategory.HairSalon)]
    [InlineData("5", ServiceCategory.Spa)]
    [InlineData("15", ServiceCategory.PetCare)]
    public void Numeric_ids_resolve(string input, ServiceCategory expected)
    {
        ServiceCategoryResolver.TryResolve(input, out var category).Should().BeTrue();
        category.Should().Be(expected);
    }

    [Theory]
    [InlineData("brows_lashes", ServiceCategory.BeautySalon)]
    [InlineData("braids_locs", ServiceCategory.HairSalon)]
    [InlineData("aesthetic_medicine", ServiceCategory.MedicalClinic)]
    [InlineData("dental_orthodontics", ServiceCategory.Dental)]
    [InlineData("hair_removal", ServiceCategory.Spa)]
    [InlineData("health_fitness", ServiceCategory.Gym)]
    public void Legacy_frontend_taxonomy_ids_still_resolve(string input, ServiceCategory expected)
    {
        // In-flight registrations must not break just because the wizard's taxonomy moved on.
        ServiceCategoryResolver.TryResolve(input, out var category).Should().BeTrue();
        category.Should().Be(expected);
    }

    [Theory]
    [InlineData("  spa  ")]
    [InlineData("SPA")]
    public void Resolution_ignores_case_and_surrounding_whitespace(string input)
    {
        ServiceCategoryResolver.TryResolve(input, out var category).Should().BeTrue();
        category.Should().Be(ServiceCategory.Spa);
    }

    [Fact]
    public void Every_category_round_trips_through_its_own_slug_and_name_and_id()
    {
        foreach (var expected in ServiceCategoryExtensions.GetAll())
        {
            foreach (var input in new[] { expected.ToSlug(), expected.ToString(), ((int)expected).ToString() })
            {
                ServiceCategoryResolver.TryResolve(input, out var category)
                    .Should().BeTrue($"'{input}' should resolve");
                category.Should().Be(expected, $"'{input}' should resolve to {expected}");
            }
        }
    }

    #endregion

    #region Rejected input

    [Theory]
    [InlineData("0")]
    [InlineData("16")]
    [InlineData("-1")]
    [InlineData("999")]
    public void Out_of_range_numbers_are_rejected(string input)
    {
        // Enum.TryParse would have accepted all of these and produced an undefined category.
        ServiceCategoryResolver.TryResolve(input, out var category).Should().BeFalse();
        category.Should().Be(default(ServiceCategory));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-category")]
    public void Unrecognised_input_is_rejected(string? input)
    {
        ServiceCategoryResolver.TryResolve(input, out var category).Should().BeFalse();
        category.Should().Be(default(ServiceCategory));
    }

    [Fact]
    public void Resolve_uses_the_fallback_only_when_the_input_is_unrecognised()
    {
        ServiceCategoryResolver.Resolve("not-a-category", ServiceCategory.BeautySalon)
            .Should().Be(ServiceCategory.BeautySalon);
        ServiceCategoryResolver.Resolve(null, ServiceCategory.BeautySalon)
            .Should().Be(ServiceCategory.BeautySalon);
        ServiceCategoryResolver.Resolve("spa", ServiceCategory.BeautySalon)
            .Should().Be(ServiceCategory.Spa);
    }

    [Fact]
    public void The_fallback_is_itself_always_a_valid_category()
    {
        ServiceCategoryResolver
            .Resolve("garbage", ServiceCategory.BeautySalon)
            .IsDefinedCategory()
            .Should().BeTrue();
    }

    #endregion
}
