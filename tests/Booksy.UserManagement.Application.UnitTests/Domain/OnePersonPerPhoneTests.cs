using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Entities;
using Booksy.UserManagement.Domain.Enums;
using FluentAssertions;

namespace Booksy.UserManagement.Application.UnitTests.Domain;

/// <summary>
/// The platform invariant: ONE PERSON PER PHONE NUMBER. When the same human appears
/// on the other side of the marketplace they must gain a capacity on their existing
/// account — never receive a second account.
/// </summary>
public class OnePersonPerPhoneTests
{
    private static User PhoneFirstPerson(UserType type)
    {
        var profile = UserProfile.Create("سارا", "احمدی", null, null, null);
        return User.RegisterWithPhone(
            Email.Create("s@booksy.test"),
            PhoneNumber.From("+989121234567"),
            profile,
            type);
    }

    [Fact]
    public void A_Customer_Signing_In_As_Provider_Becomes_Both_And_Keeps_One_Account()
    {
        var person = PhoneFirstPerson(UserType.Customer);
        var originalId = person.Id;

        var changed = person.EnsureCanActAs(UserType.Provider);

        changed.Should().BeTrue();
        person.Id.Should().Be(originalId, "the same person must be reused, never duplicated");
        person.Type.Should().Be(UserType.Both);
        person.HasRole("Customer").Should().BeTrue();
        person.HasRole("Provider").Should().BeTrue();
        person.CanActAs(UserType.Customer).Should().BeTrue();
        person.CanActAs(UserType.Provider).Should().BeTrue();
    }

    [Fact]
    public void A_Provider_Booking_As_A_Customer_Becomes_Both()
    {
        var person = PhoneFirstPerson(UserType.Provider);

        person.EnsureCanActAs(UserType.Customer).Should().BeTrue();

        person.Type.Should().Be(UserType.Both);
        person.CanActAs(UserType.Customer).Should().BeTrue();
    }

    [Fact]
    public void Granting_An_Existing_Capacity_Is_Idempotent()
    {
        var person = PhoneFirstPerson(UserType.Provider);
        var rolesBefore = person.Roles.Count;

        person.EnsureCanActAs(UserType.Provider).Should().BeFalse("nothing changes");

        person.Type.Should().Be(UserType.Provider);
        person.Roles.Should().HaveCount(rolesBefore);
    }

    [Fact]
    public void Both_Already_Covers_Every_Capacity()
    {
        var person = PhoneFirstPerson(UserType.Customer);
        person.EnsureCanActAs(UserType.Provider);

        person.EnsureCanActAs(UserType.Customer).Should().BeFalse();
        person.EnsureCanActAs(UserType.Provider).Should().BeFalse();
        person.Type.Should().Be(UserType.Both);
    }

    [Theory]
    [InlineData(UserType.Admin)]
    [InlineData(UserType.Support)]
    public void Elevated_Capacities_Are_Never_Inferred(UserType elevated)
    {
        var person = PhoneFirstPerson(UserType.Customer);

        person.EnsureCanActAs(elevated).Should().BeFalse();

        person.Type.Should().Be(UserType.Customer, "Admin/Support are granted deliberately");
    }

    [Fact]
    public void A_PhoneFirst_Person_Stores_The_Canonical_Phone_On_The_Account()
    {
        // Local Iranian form must land as canonical E.164 on the ACCOUNT (not only the
        // profile) — otherwise the account is invisible to phone lookup and a later
        // sign-in would mint a duplicate person.
        var profile = UserProfile.Create("رضا", "م", null, null, null);
        var person = User.RegisterWithPhone(
            Email.Create("r@booksy.test"),
            PhoneNumber.From("09121234567"),
            profile,
            UserType.Provider);

        person.PhoneNumber.Should().NotBeNull();
        person.PhoneNumber!.Value.Should().Be("+989121234567");
        person.PhoneNumberVerified.Should().BeTrue();
    }
}
