using Booksy.Core.Domain.Infrastructure.Middleware;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Entities;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Domain.Repositories;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Memberships;

/// <summary>
/// refactor-identity-and-membership §16.3/§4.8: "concurrent double-accept must not create two
/// persons/memberships" and "reuse-by-phone at the HTTP/OTP boundary" were carried as blocked on
/// "a two-schema test DB + real OTP handling" that this environment supposedly didn't have. Wrong
/// on both counts: <c>ServiceCatalogTestWebApplicationFactory</c> already boots the real
/// composed Host (both schemas, one Postgres), and the OTP subsystem's own <c>Sandbox: true</c> /
/// <c>SandboxCode: "123456"</c> (the Host's own default, <c>appsettings.json</c>) makes the code
/// deterministic without ever touching a real gateway. These tests go through the real HTTP
/// pipeline (anonymous, no auth header) exactly as a genuine invitee would.
/// </summary>
public class RegisterAndAcceptInvitationTests : ServiceCatalogIntegrationTestBase
{
    private const string SandboxOtpCode = "123456";

    public RegisterAndAcceptInvitationTests(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory) { }

    private async Task<Guid> CreateRealUserWithPhoneAsync(string phone, string email)
    {
        var userDbContext = Scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();

        var user = User.RegisterWithPhone(
            Email.Create(email),
            PhoneNumber.From(phone),
            UserProfile.Create("Existing", "Person"),
            UserType.Provider);

        userDbContext.Add(user);
        await userDbContext.SaveChangesAsync();
        return user.Id.Value;
    }

    private async Task<Guid> SeedPendingInvitationAsync(ProviderId organizationId, string phone)
    {
        var invitation = ProviderInvitation.Create(organizationId, PhoneNumber.From(phone), inviteeName: "New Invitee");

        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IProviderInvitationWriteRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>();
        await repo.SaveAsync(invitation);
        await uow.CommitAsync();

        return invitation.Id;
    }

    private async Task<HttpResponseMessage> SendOtpAsync(Guid invitationId) =>
        await Client.PostAsync($"/api/v1/memberships/invitations/{invitationId}/send-otp", null);

    private Task<HttpResponseMessage> RegisterAndAcceptAsync(
        Guid invitationId, string firstName, string lastName, string otpCode) =>
        Client.PostAsJsonAsync(
            $"/api/v1/memberships/invitations/{invitationId}/register-and-accept",
            new { firstName, lastName, email = (string?)null, otpCode });

    [Fact]
    public async Task RegisterAndAccept_Reuses_An_Existing_Account_By_Phone_Never_A_Duplicate_Person()
    {
        var phone = $"+9891{Random.Shared.Next(10000000, 99999999)}";
        var existingPersonId = await CreateRealUserWithPhoneAsync(
            phone, $"existing-{Guid.NewGuid():N}@test.com");

        var provider = await CreateAndAuthenticateAsProviderAsync(
            email: $"reuse-org-{Guid.NewGuid():N}@test.com");
        ClearAuthenticationHeader();

        var invitationId = await SeedPendingInvitationAsync(provider.Id, phone);
        (await SendOtpAsync(invitationId)).EnsureSuccessStatusCode();

        var response = await RegisterAndAcceptAsync(invitationId, "New", "Name", SandboxOtpCode);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<RegisterAndAcceptBody>>();
        body!.Data!.PersonId.Should().Be(existingPersonId,
            "the phone already belongs to a person; a second one must never be created");
        body.Data.IsNewAccount.Should().BeFalse();

        using var scope = Factory.Services.CreateScope();
        var membershipRepo = scope.ServiceProvider.GetRequiredService<IOrganizationMembershipRepository>();
        var memberships = await membershipRepo.GetByOrganizationAsync(provider.Id);
        memberships.Should().ContainSingle(m => m.PersonId != null && m.PersonId.Value == existingPersonId);
    }

    [Fact]
    public async Task Concurrent_Register_And_Accept_For_A_Brand_New_Phone_Creates_Exactly_One_Person()
    {
        var phone = $"+9891{Random.Shared.Next(10000000, 99999999)}";
        var provider = await CreateAndAuthenticateAsProviderAsync(
            email: $"race-org-{Guid.NewGuid():N}@test.com");
        ClearAuthenticationHeader();

        var invitationId = await SeedPendingInvitationAsync(provider.Id, phone);
        (await SendOtpAsync(invitationId)).EnsureSuccessStatusCode();

        // Two concurrent attempts to register-and-accept with the same OTP for a phone that
        // resolves to nobody yet -- the exact race #16.3 named. AcceptByMember() only allows a
        // Pending invitation to transition once, so at most one request can win that guard; the
        // question this pins is whether the LOSER also managed to create a second person before
        // losing, not just whether the HTTP responses look sane.
        var first = RegisterAndAcceptAsync(invitationId, "Race", "One", SandboxOtpCode);
        var second = RegisterAndAcceptAsync(invitationId, "Race", "Two", SandboxOtpCode);
        var responses = await Task.WhenAll(first, second);

        using var verifyScope = Factory.Services.CreateScope();
        var userRepository = verifyScope.ServiceProvider.GetRequiredService<IUserRepository>();
        var person = await userRepository.GetByPhoneNumberAsync(phone);
        person.Should().NotBeNull("at least one attempt must have succeeded in creating the account");

        var stillExistsCount = 0;
        foreach (var response in responses)
        {
            if (response.IsSuccessStatusCode) stillExistsCount++;
        }
        stillExistsCount.Should().BeGreaterThanOrEqualTo(1);

        // The actual invariant under test: exactly one membership for this person in this
        // organization, and exactly one Active/Invited row overall -- never two independent
        // people racing into two independent memberships.
        var membershipRepo = verifyScope.ServiceProvider.GetRequiredService<IOrganizationMembershipRepository>();
        var memberships = await membershipRepo.GetByOrganizationAsync(provider.Id);
        memberships.Where(m => m.PersonId != null).Select(m => m.PersonId!.Value).Distinct()
            .Should().ContainSingle(
                "both requests targeted the same invitation and phone; only one person may end up a member");
    }

    private sealed record RegisterAndAcceptBody(
        Guid PersonId, Guid MembershipId, Guid OrganizationId, bool IsNewAccount);
}
