using Booksy.ServiceCatalog.Application.Commands.Membership;
using Booksy.ServiceCatalog.Application.Commands.Membership.ChangeMembershipRoles;
using Booksy.ServiceCatalog.Application.Commands.Membership.RegisterAndAcceptInvitation;
using Booksy.ServiceCatalog.Application.Commands.Membership.RevokeInvitation;
using Booksy.ServiceCatalog.Application.Commands.Membership.TerminateMembership;
using FluentAssertions;

namespace Booksy.ServiceCatalog.Application.UnitTests.Commands.Membership;

/// <summary>
/// Boundary validation for the membership commands. These run in the MediatR
/// pipeline before the handler, so malformed input never reaches domain logic —
/// and a request can never be rejected by the database instead of the API.
/// </summary>
public class MembershipCommandValidatorsTests
{
    [Fact]
    public void RevokeInvitation_Requires_An_Invitation_And_Bounds_The_Reason()
    {
        var validator = new RevokeInvitationCommandValidator();

        validator.Validate(new RevokeInvitationCommand(Guid.Empty, null))
            .IsValid.Should().BeFalse("the invitation id is required");

        validator.Validate(new RevokeInvitationCommand(Guid.NewGuid(), new string('x', 501)))
            .IsValid.Should().BeFalse("the reason must fit the column");

        validator.Validate(new RevokeInvitationCommand(Guid.NewGuid(), "wrong number"))
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public void TerminateMembership_Requires_A_Membership()
    {
        var validator = new TerminateMembershipCommandValidator();

        validator.Validate(new TerminateMembershipCommand(Guid.Empty, null)).IsValid.Should().BeFalse();
        validator.Validate(new TerminateMembershipCommand(Guid.NewGuid(), "left")).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ChangeRoles_Rejects_An_Empty_Set_And_Unknown_Roles()
    {
        var validator = new ChangeMembershipRolesCommandValidator();

        validator.Validate(new ChangeMembershipRolesCommand(Guid.NewGuid(), Array.Empty<string>()))
            .IsValid.Should().BeFalse("clearing every role must be a termination, not a role change");

        validator.Validate(new ChangeMembershipRolesCommand(Guid.NewGuid(), new[] { "Sorcerer" }))
            .IsValid.Should().BeFalse("unknown roles are rejected at the boundary");

        validator.Validate(new ChangeMembershipRolesCommand(Guid.NewGuid(), new[] { "Owner", "StaffProvider" }))
            .IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Doe", "123456", false)]      // first name required
    [InlineData("Jane", "", "123456", false)]     // last name required
    [InlineData("Jane", "Doe", "", false)]        // otp required
    [InlineData("Jane", "Doe", "abc123", false)]  // otp must be digits
    [InlineData("Jane", "Doe", "123456", true)]
    public void RegisterAndAccept_Validates_Identity_And_Otp(
        string first, string last, string otp, bool expected)
    {
        var validator = new RegisterAndAcceptInvitationCommandValidator();

        var result = validator.Validate(
            new RegisterAndAcceptInvitationCommand(Guid.NewGuid(), first, last, null, otp));

        result.IsValid.Should().Be(expected);
    }

    [Fact]
    public void RegisterAndAccept_Rejects_A_Malformed_Email_But_Allows_None()
    {
        var validator = new RegisterAndAcceptInvitationCommandValidator();

        validator.Validate(new RegisterAndAcceptInvitationCommand(
            Guid.NewGuid(), "Jane", "Doe", "not-an-email", "123456")).IsValid.Should().BeFalse();

        // Email stays optional — these accounts are phone-first.
        validator.Validate(new RegisterAndAcceptInvitationCommand(
            Guid.NewGuid(), "Jane", "Doe", null, "123456")).IsValid.Should().BeTrue();
    }
}
