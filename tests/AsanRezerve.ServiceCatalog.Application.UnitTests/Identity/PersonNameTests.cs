using AsanRezerve.ServiceCatalog.Application.Abstractions.Identity;
using FluentAssertions;
using Xunit;

namespace AsanRezerve.ServiceCatalog.Application.UnitTests.Identity;

/// <summary>
/// A person created by OTP with no name is stored as «مشتری 9123456789» / «ارائه‌دهنده 9123456789»
/// (PersonProvisioningService). Printing that is what put a phone number where the booking summary says
/// «متخصص», and «مشتری گرامی» where a name belonged (QA walkthrough 2026-09-22).
/// </summary>
public class PersonNameTests
{
    [Fact]
    public void A_real_name_is_first_and_last_together()
    {
        PersonName.RealOrNull("سارا", "احمدی").Should().Be("سارا احمدی");
        PersonName.RealOrNull(" سارا ", " احمدی ").Should().Be("سارا احمدی");
    }

    [Fact]
    public void One_half_of_a_name_is_still_a_name()
    {
        PersonName.RealOrNull("سارا", null).Should().Be("سارا");
        PersonName.RealOrNull(null, "احمدی").Should().Be("احمدی");
    }

    [Theory]
    [InlineData("مشتری", "9384444636")]
    [InlineData("ارائه‌دهنده", "9123135143")]
    [InlineData("ارائه دهنده", "9123135143")]
    public void A_placeholder_is_not_a_name(string first, string last)
    {
        PersonName.RealOrNull(first, last).Should().BeNull();
    }

    [Fact]
    public void A_number_is_never_a_surname()
    {
        // The placeholder's surname is the national number; a real first name must not drag it along.
        PersonName.RealOrNull("سارا", "9123135143").Should().Be("سارا");
    }

    [Fact]
    public void Nothing_at_all_is_no_name()
    {
        PersonName.RealOrNull(null, null).Should().BeNull();
        PersonName.RealOrNull("  ", "").Should().BeNull();
    }

    // ---- QA 2026-09-23: "the number must never be written anywhere" ----

    [Theory]
    [InlineData("سارا", "+989123135143")]
    [InlineData("سارا", "0912 313 5143")]
    [InlineData("سارا", "۰۹۱۲۳۱۳۵۱۴۳")]
    public void A_phone_in_any_spelling_is_never_a_surname(string first, string last)
    {
        PersonName.RealOrNull(first, last).Should().Be("سارا");
    }

    [Theory]
    [InlineData("09123135143", null)]
    [InlineData("+98 912 313 5143", "")]
    [InlineData("ارائه‌دهنده 9123135143", null)]
    [InlineData("ارائه‌دهنده 9123135143", "9123135143")]
    [InlineData("مشتری", "۹۳۸۴۴۴۴۶۳۶")]
    public void A_phone_or_the_whole_placeholder_in_the_first_name_field_is_no_name(string first, string? last)
    {
        PersonName.RealOrNull(first, last).Should().BeNull();
    }

    [Theory]
    [InlineData("ارائه‌دهنده 9123135143")]
    [InlineData("ارائه دهنده 9123135143")]
    [InlineData("مشتری 9384444636")]
    [InlineData("ارائه‌دهنده")]
    [InlineData("09123135143")]
    [InlineData("+98 912 313 5143")]
    [InlineData("   ")]
    [InlineData(null)]
    public void A_single_name_string_that_is_a_placeholder_or_a_phone_is_no_name(string? name)
    {
        PersonName.Sanitize(name).Should().BeNull();
    }

    [Theory]
    [InlineData("مریم", "مریم")]
    [InlineData(" مریم رضایی ", "مریم رضایی")]
    [InlineData("سالن ۲۴ ساعته", "سالن ۲۴ ساعته")]
    [InlineData("مریم 09123135143", "مریم")]
    public void A_real_name_keeps_everything_but_a_phone_number(string name, string expected)
    {
        // Short numbers are part of names people choose; seven digits or more is a phone number.
        PersonName.Sanitize(name).Should().Be(expected);
    }

    [Fact]
    public void A_member_is_named_by_their_person_first()
    {
        var person = new PersonInfo(Guid.NewGuid(), "سارا", "احمدی", "+989123135143", "Active");

        PersonName.ForMember(person, "ساری", "سالن نهال").Should().Be("سارا احمدی");
    }

    [Fact]
    public void A_member_with_only_the_placeholder_is_named_by_the_salons_name_for_them_then_the_salon()
    {
        var placeholder = new PersonInfo(Guid.NewGuid(), "ارائه‌دهنده", "9123135143", "+989123135143", "Active");

        PersonName.ForMember(placeholder, "مریم", "سالن نهال").Should().Be("مریم");
        PersonName.ForMember(placeholder, null, "سالن نهال").Should().Be("سالن نهال");
        PersonName.ForMember(placeholder, "09121112222", "سالن نهال").Should().Be("سالن نهال",
            "a display name the salon typed as a phone number is still a phone number");
        PersonName.ForMember(null, null, "سالن نهال").Should().Be("سالن نهال");
    }

    [Fact]
    public void The_parts_of_a_placeholder_are_blank()
    {
        PersonName.RealParts("ارائه‌دهنده", "9123135143").Should().Be((string.Empty, string.Empty));
        PersonName.RealParts("سارا", "9123135143").Should().Be(("سارا", string.Empty));
        PersonName.RealParts(" سارا ", " احمدی ").Should().Be(("سارا", "احمدی"));
    }

    [Theory]
    [InlineData("ناصر", "عابدی", "ناصر ع.")]
    [InlineData(" سارا ", " احمدی ", "سارا ا.")]
    [InlineData("مریم", null, "مریم")]
    [InlineData("مریم", "9123135143", "مریم")]
    [InlineData("مشتری", "9123135143", "مشتری")]
    [InlineData(null, null, "مشتری")]
    [InlineData(null, "عابدی", "مشتری")]
    public void A_public_review_is_signed_with_first_name_and_surname_initial(string? first, string? last, string expected)
    {
        PersonName.ForPublicReview(first, last).Should().Be(expected,
            "enough to read as a person, not enough to find them — never a placeholder, a phone or an id");
    }
}
