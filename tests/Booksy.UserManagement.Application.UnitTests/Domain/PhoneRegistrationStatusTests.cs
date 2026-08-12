using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Entities;
using Booksy.UserManagement.Domain.Enums;
using FluentAssertions;

namespace Booksy.UserManagement.Application.UnitTests.Domain;

/// <summary>
/// Every Booksy account is created by OTP, so <see cref="User.RegisterWithPhone"/> decides the
/// starting <see cref="UserStatus"/> for the entire user base.
///
/// <para>It briefly set <c>Draft</c> — swept in by a commit whose subject was "Centralize utility
/// services and refactor 24+ components" (a frontend price/string/phone/date-service refactor
/// across 90 files), which flipped this one domain line and left the original rationale comment
/// in place, corrupted to "Immediately darft since phone is verified". <c>Draft</c> is a real
/// enum member, so nothing failed loudly; it surfaced only as <c>"user-status": "Draft"</c> in
/// the JWT of a provider whose profile was already Active.</para>
///
/// <para>The damage was that <c>Draft</c> is terminal for an OTP account. Suspend, Deactivate,
/// ChangePassword, RequestPasswordReset and Authenticate all require Active, and neither route
/// out of Draft is reachable: <c>Activate(token)</c> needs the email activation token this
/// factory sets to null, and <c>VerifyPhoneNumber()</c> — the one method that promotes a phone
/// user — short-circuits because registration already marks the phone verified. Admins therefore
/// could not suspend or deactivate any phone-registered person on the platform.</para>
/// </summary>
public class PhoneRegistrationStatusTests
{
    private static User PhoneRegistered(UserType type = UserType.Provider) =>
        User.RegisterWithPhone(
            Email.Create("s@booksy.test"),
            PhoneNumber.From("+989121234567"),
            UserProfile.Create("سارا", "احمدی", null, null, null),
            type);

    [Theory]
    [InlineData(UserType.Provider)]
    [InlineData(UserType.Customer)]
    public void An_OTP_Registered_Person_Starts_Active(UserType type)
    {
        PhoneRegistered(type).Status.Should().Be(
            UserStatus.Active,
            "the phone is proven by OTP before this factory runs, and ActivatedAt is stamped here");
    }

    /// <summary>
    /// Guards the internal contradiction the regression introduced: a person stamped with an
    /// activation timestamp while being held in a pre-activation status.
    /// </summary>
    [Fact]
    public void Status_Agrees_With_ActivatedAt()
    {
        var person = PhoneRegistered();

        person.ActivatedAt.Should().NotBeNull();
        person.Status.Should().Be(UserStatus.Active, "ActivatedAt is set, so the status must say so too");
    }

    /// <summary>
    /// The consequence that actually mattered. Both moderation paths assert Active internally,
    /// so under the regression these threw for every account on the platform.
    /// </summary>
    [Fact]
    public void An_OTP_Registered_Person_Can_Be_Suspended_By_An_Admin()
    {
        var person = PhoneRegistered();

        var suspend = () => person.Suspend("abuse report", DateTime.UtcNow.AddDays(7));

        suspend.Should().NotThrow("admins must be able to moderate phone-registered accounts");
        person.Status.Should().Be(UserStatus.Suspended);
    }

    [Fact]
    public void An_OTP_Registered_Person_Can_Be_Deactivated()
    {
        var person = PhoneRegistered();

        var deactivate = () => person.Deactivate("user requested closure");

        deactivate.Should().NotThrow();
        person.Status.Should().Be(UserStatus.Inactive);
    }

    /// <summary>
    /// The OTP sign-in gate rejects Banned/Suspended/Inactive. Active must not be caught by it —
    /// otherwise fixing the starting status would lock everyone out instead.
    /// </summary>
    [Fact]
    public void The_Starting_Status_Is_Not_One_The_Sign_In_Gate_Rejects()
    {
        var status = PhoneRegistered().Status;

        new[] { UserStatus.Banned, UserStatus.Suspended, UserStatus.Inactive }
            .Should().NotContain(status);
    }
}
