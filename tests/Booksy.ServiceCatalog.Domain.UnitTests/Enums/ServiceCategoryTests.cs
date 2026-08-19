using System.Text.RegularExpressions;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Enums.Extensions;

namespace Booksy.ServiceCatalog.Domain.UnitTests.Enums;

/// <summary>
/// Unit tests for the ServiceCategory enum and its display metadata.
///
/// The integer values are the database contract (providers.PrimaryCategory and
/// services.Category are stored as ints) and the wire contract shared with the Vue
/// frontend, so these tests pin the numbers as much as the names. Renumbering a
/// category silently re-labels every existing row, which is exactly the regression
/// this file exists to catch.
/// </summary>
public class ServiceCategoryTests
{
    public static TheoryData<ServiceCategory> AllCategories()
    {
        var data = new TheoryData<ServiceCategory>();
        foreach (var category in ServiceCategoryExtensions.GetAll())
            data.Add(category);
        return data;
    }

    #region Enum completeness and stable identifiers

    [Fact]
    public void Enum_Declares_Exactly_The_Fifteen_Agreed_Categories()
    {
        var categories = ServiceCategoryExtensions.GetAll();

        Assert.Equal(15, categories.Length);
        Assert.Equal(
            new[]
            {
                ServiceCategory.HairSalon,
                ServiceCategory.Barbershop,
                ServiceCategory.BeautySalon,
                ServiceCategory.NailSalon,
                ServiceCategory.Spa,
                ServiceCategory.Massage,
                ServiceCategory.Gym,
                ServiceCategory.Yoga,
                ServiceCategory.MedicalClinic,
                ServiceCategory.Dental,
                ServiceCategory.Physiotherapy,
                ServiceCategory.Tutoring,
                ServiceCategory.Automotive,
                ServiceCategory.HomeServices,
                ServiceCategory.PetCare
            },
            categories);
    }

    [Theory]
    [InlineData(ServiceCategory.HairSalon, 1)]
    [InlineData(ServiceCategory.Barbershop, 2)]
    [InlineData(ServiceCategory.BeautySalon, 3)]
    [InlineData(ServiceCategory.NailSalon, 4)]
    [InlineData(ServiceCategory.Spa, 5)]
    [InlineData(ServiceCategory.Massage, 6)]
    [InlineData(ServiceCategory.Gym, 7)]
    [InlineData(ServiceCategory.Yoga, 8)]
    [InlineData(ServiceCategory.MedicalClinic, 9)]
    [InlineData(ServiceCategory.Dental, 10)]
    [InlineData(ServiceCategory.Physiotherapy, 11)]
    [InlineData(ServiceCategory.Tutoring, 12)]
    [InlineData(ServiceCategory.Automotive, 13)]
    [InlineData(ServiceCategory.HomeServices, 14)]
    [InlineData(ServiceCategory.PetCare, 15)]
    public void Each_Category_Keeps_Its_Persisted_Integer_Id(ServiceCategory category, int expectedId)
    {
        Assert.Equal(expectedId, (int)category);
    }

    [Fact]
    public void Zero_Is_Not_A_Category_So_Unset_Columns_Are_Detectable()
    {
        // The migration that introduced PrimaryCategory backfilled 0. Keeping 0 outside the
        // enum is what lets the remediation migration and the aggregate guards spot those rows.
        Assert.False(Enum.IsDefined((ServiceCategory)0));
        Assert.DoesNotContain(0, ServiceCategoryExtensions.GetAll().Select(c => (int)c));
    }

    [Fact]
    public void Category_Ids_Are_Unique()
    {
        var ids = ServiceCategoryExtensions.GetAll().Select(c => (int)c).ToArray();

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    #endregion

    #region Metadata coverage — every category, every accessor

    [Theory]
    [MemberData(nameof(AllCategories))]
    public void Every_Category_Exposes_Complete_Display_Metadata(ServiceCategory category)
    {
        Assert.False(string.IsNullOrWhiteSpace(category.ToPersianName()));
        Assert.False(string.IsNullOrWhiteSpace(category.ToEnglishName()));
        Assert.False(string.IsNullOrWhiteSpace(category.ToIcon()));
        Assert.False(string.IsNullOrWhiteSpace(category.ToColorHex()));
        Assert.False(string.IsNullOrWhiteSpace(category.ToGradient()));
        Assert.False(string.IsNullOrWhiteSpace(category.ToSlug()));
        Assert.False(string.IsNullOrWhiteSpace(category.ToDescription()));
    }

    [Theory]
    [MemberData(nameof(AllCategories))]
    public void Every_Category_Has_A_Six_Digit_Hex_Colour(ServiceCategory category)
    {
        Assert.Matches("^#[0-9A-Fa-f]{6}$", category.ToColorHex());
    }

    [Theory]
    [MemberData(nameof(AllCategories))]
    public void Every_Category_Has_A_Css_Gradient_Referencing_Its_Brand_Colour(ServiceCategory category)
    {
        var gradient = category.ToGradient();

        Assert.StartsWith("linear-gradient(", gradient);
        Assert.Contains(category.ToColorHex(), gradient, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(AllCategories))]
    public void Every_Category_Has_A_Url_Safe_Slug(ServiceCategory category)
    {
        Assert.Matches("^[a-z]+(-[a-z]+)*$", category.ToSlug());
    }

    [Theory]
    [MemberData(nameof(AllCategories))]
    public void Every_Category_Has_A_Persian_Name_Written_In_Persian(ServiceCategory category)
    {
        // Guards against an English placeholder leaking into the Persian mapping.
        Assert.Matches(@"\p{IsArabic}", category.ToPersianName());
    }

    [Fact]
    public void Slugs_Are_Unique_So_Category_Urls_Cannot_Collide()
    {
        var slugs = ServiceCategoryExtensions.GetAll().Select(c => c.ToSlug()).ToArray();

        Assert.Equal(slugs.Length, slugs.Distinct().Count());
    }

    [Fact]
    public void Persian_And_English_Names_Are_Unique_So_Categories_Stay_Distinguishable()
    {
        var categories = ServiceCategoryExtensions.GetAll();

        var persian = categories.Select(c => c.ToPersianName()).ToArray();
        var english = categories.Select(c => c.ToEnglishName()).ToArray();

        Assert.Equal(persian.Length, persian.Distinct().Count());
        Assert.Equal(english.Length, english.Distinct().Count());
    }

    [Theory]
    [InlineData(ServiceCategory.HairSalon, "آرایشگاه زنانه", "💇‍♀️", "#8B5CF6", "hair-salon")]
    [InlineData(ServiceCategory.Barbershop, "آرایشگاه مردانه", "💇‍♂️", "#3B82F6", "barbershop")]
    [InlineData(ServiceCategory.Spa, "اسپا", "🧖", "#06B6D4", "spa")]
    [InlineData(ServiceCategory.MedicalClinic, "کلینیک پزشکی", "🏥", "#EF4444", "medical-clinic")]
    [InlineData(ServiceCategory.PetCare, "مراقبت حیوانات", "🐾", "#FBBF24", "pet-care")]
    public void Spot_Checked_Categories_Render_The_Agreed_Metadata(
        ServiceCategory category,
        string persianName,
        string icon,
        string colorHex,
        string slug)
    {
        Assert.Equal(persianName, category.ToPersianName());
        Assert.Equal(icon, category.ToIcon());
        Assert.Equal(colorHex, category.ToColorHex());
        Assert.Equal(slug, category.ToSlug());
    }

    #endregion

    #region Undefined values

    [Theory]
    [InlineData(0)]
    [InlineData(16)]
    [InlineData(-1)]
    [InlineData(999)]
    public void Metadata_Accessors_Reject_Undefined_Values(int rawValue)
    {
        var undefined = (ServiceCategory)rawValue;

        Assert.Throws<ArgumentOutOfRangeException>(() => undefined.ToPersianName());
        Assert.Throws<ArgumentOutOfRangeException>(() => undefined.ToEnglishName());
        Assert.Throws<ArgumentOutOfRangeException>(() => undefined.ToIcon());
        Assert.Throws<ArgumentOutOfRangeException>(() => undefined.ToColorHex());
        Assert.Throws<ArgumentOutOfRangeException>(() => undefined.ToGradient());
        Assert.Throws<ArgumentOutOfRangeException>(() => undefined.ToSlug());
        Assert.Throws<ArgumentOutOfRangeException>(() => undefined.ToDescription());
    }

    [Theory]
    [MemberData(nameof(AllCategories))]
    public void IsDefinedCategory_Accepts_Declared_Values(ServiceCategory category)
    {
        Assert.True(category.IsDefinedCategory());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(16)]
    [InlineData(-1)]
    [InlineData(999)]
    public void IsDefinedCategory_Rejects_Undefined_Values(int rawValue)
    {
        Assert.False(((ServiceCategory)rawValue).IsDefinedCategory());
    }

    #endregion

    #region Slug parsing

    [Theory]
    [MemberData(nameof(AllCategories))]
    public void Slug_Round_Trips_Back_To_The_Same_Category(ServiceCategory category)
    {
        Assert.True(ServiceCategoryExtensions.TryParseSlug(category.ToSlug(), out var parsed));
        Assert.Equal(category, parsed);
    }

    [Theory]
    [InlineData("hair_salon", ServiceCategory.HairSalon)]
    [InlineData("barber", ServiceCategory.Barbershop)]
    [InlineData("beauty", ServiceCategory.BeautySalon)]
    [InlineData("nails", ServiceCategory.NailSalon)]
    [InlineData("fitness", ServiceCategory.Gym)]
    [InlineData("clinic", ServiceCategory.MedicalClinic)]
    [InlineData("physio", ServiceCategory.Physiotherapy)]
    [InlineData("education", ServiceCategory.Tutoring)]
    [InlineData("auto", ServiceCategory.Automotive)]
    [InlineData("pet", ServiceCategory.PetCare)]
    public void Legacy_Frontend_Aliases_Still_Parse(string alias, ServiceCategory expected)
    {
        Assert.True(ServiceCategoryExtensions.TryParseSlug(alias, out var parsed));
        Assert.Equal(expected, parsed);
    }

    [Theory]
    [InlineData("HAIR-SALON")]
    [InlineData("  spa  ")]
    [InlineData("Barbershop")]
    public void Slug_Parsing_Ignores_Case_And_Surrounding_Whitespace(string slug)
    {
        Assert.True(ServiceCategoryExtensions.TryParseSlug(slug, out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("not-a-category")]
    [InlineData("0")]
    [InlineData("1")]
    public void Unknown_Slugs_Fail_To_Parse_And_Yield_No_Category(string? slug)
    {
        // "0" previously slipped through and produced the undefined category 0.
        Assert.False(ServiceCategoryExtensions.TryParseSlug(slug, out var parsed));
        Assert.Equal(default, parsed);
    }

    #endregion
}
