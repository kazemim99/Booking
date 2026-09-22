using Booksy.ServiceCatalog.Application.Abstractions.Identity;
using FluentAssertions;
using Xunit;

namespace Booksy.ServiceCatalog.Application.UnitTests.Identity;

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
}
