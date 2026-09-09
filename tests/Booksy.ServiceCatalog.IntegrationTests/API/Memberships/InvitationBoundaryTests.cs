using Booksy.Core.Domain.Infrastructure.Middleware;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Entities;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Memberships;

/// <summary>
/// refactor-identity-and-membership §5.5b. The self-invite guard and reuse-by-phone were proven
/// at the application layer (<c>SendInvitationCommandHandlerTests</c>, §4.1/§4.8) and, for the
/// register-and-accept path, at the HTTP boundary (§16.3). What was still missing is the plain
/// invite → accept path through the real pipeline: the phone is resolved by the real
/// <c>IPersonDirectory</c> read over <c>user_management.users</c>, the invitation is created by
/// the real handler, and the invitee accepts as an authenticated caller.
///
/// Spec scenarios (organization-membership / provider-staff-management):
/// "Prevent self-invitation", "Self-invite is refused", "Invite a phone that already has an
/// account", "Existing user accepts".
/// </summary>
public class InvitationBoundaryTests : ServiceCatalogIntegrationTestBase
{
    public InvitationBoundaryTests(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory) { }

    private static string RandomPhone() => $"+9891{Random.Shared.Next(10000000, 99999999)}";

    private async Task<Guid> CreateRealUserWithPhoneAsync(string phone, string email)
    {
        var userDbContext = Scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();

        var user = User.RegisterWithPhone(
            Email.Create(email),
            PhoneNumber.From(phone),
            UserProfile.Create("Real", "Person"),
            UserType.Provider);

        userDbContext.Add(user);
        await userDbContext.SaveChangesAsync();
        return user.Id.Value;
    }

    private Task<HttpResponseMessage> InviteAsync(Guid organizationId, string phone, string? name = null) =>
        Client.PostAsJsonAsync(
            $"/api/v1/providers/{organizationId}/hierarchy/invitations",
            new { inviteePhoneNumber = phone, inviteeName = name, message = (string?)null });

    [Fact]
    public async Task An_Owner_Cannot_Invite_Their_Own_Phone_Number()
    {
        var ownerPhone = RandomPhone();
        var ownerEmail = $"owner-{Guid.NewGuid():N}@test.com";
        var ownerId = await CreateRealUserWithPhoneAsync(ownerPhone, ownerEmail);
        var salon = await CreateAndAuthenticateAsProviderAsync("Self Invite Salon", ownerEmail, ownerId);

        var response = await InviteAsync(salon.Id.Value, ownerPhone);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "the owner's own phone resolves to the owner, and the guard refuses it before any invitation exists");

        using var scope = Factory.Services.CreateScope();
        var invitations = scope.ServiceProvider.GetRequiredService<IProviderInvitationReadRepository>();
        (await invitations.GetByOrganizationIdAsync(salon.Id)).Should().BeEmpty(
            "a refused self-invite must leave nothing behind");
    }

    [Fact]
    public async Task Inviting_A_Phone_That_Already_Has_An_Account_Reuses_That_Person_On_Accept()
    {
        var ownerEmail = $"owner-{Guid.NewGuid():N}@test.com";
        var ownerId = await CreateRealUserWithPhoneAsync(RandomPhone(), ownerEmail);
        var salon = await CreateAndAuthenticateAsProviderAsync("Reuse Salon", ownerEmail, ownerId);

        var inviteePhone = RandomPhone();
        var inviteeEmail = $"invitee-{Guid.NewGuid():N}@test.com";
        var inviteeId = await CreateRealUserWithPhoneAsync(inviteePhone, inviteeEmail);

        // The owner invites a phone that already belongs to someone.
        var inviteResponse = await InviteAsync(salon.Id.Value, inviteePhone, "Existing Stylist");
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var invitation = await inviteResponse.Content.ReadFromJsonAsync<ApiResponse<InvitationBody>>();
        invitation!.Data!.InvitationId.Should().NotBeEmpty();

        // That person accepts as themselves.
        AuthenticateAsUser(inviteeId, inviteeEmail);
        var acceptResponse = await Client.PostAsync(
            $"/api/v1/memberships/invitations/{invitation.Data.InvitationId}/accept", null);
        acceptResponse.EnsureSuccessStatusCode();

        using var scope = Factory.Services.CreateScope();
        var memberships = scope.ServiceProvider.GetRequiredService<IOrganizationMembershipRepository>();
        (await memberships.GetByOrganizationAsync(salon.Id))
            .Should().ContainSingle(m => m.PersonId != null && m.PersonId.Value == inviteeId,
                "accepting must attach the membership to the existing account, never a new one");

        var users = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();
        (await users.Users.CountAsync(u => u.PhoneNumber != null && u.PhoneNumber.Value == inviteePhone))
            .Should().Be(1, "one person per phone: the invite → accept path must not create a second account");
    }

    private sealed record InvitationBody(Guid InvitationId, Guid OrganizationId, string PhoneNumber);
}
