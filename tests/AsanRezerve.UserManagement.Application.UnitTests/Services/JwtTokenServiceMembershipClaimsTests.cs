using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.UserManagement.Application.Services.Interfaces;
using AsanRezerve.UserManagement.Domain.Enums;
using AsanRezerve.UserManagement.Infrastructure.Services.Security;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Xunit;

namespace AsanRezerve.UserManagement.Application.UnitTests.Services;

/// <summary>
/// refactor-identity-and-membership §5.4: a token must carry the caller's memberships and
/// which one is "active", so the provider app can source its salon switcher and roster from
/// the JWT rather than a second round-trip. No database is needed here -- these tests exercise
/// JwtTokenService's own claim-emission logic directly.
/// </summary>
public class JwtTokenServiceMembershipClaimsTests
{
    private static JwtTokenService CreateService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "AsanRezerve.Tests",
                ["Jwt:Audience"] = "AsanRezerve.Tests.Users",
                ["Jwt:SecretKey"] = "this-is-a-test-only-secret-key-at-least-32-bytes-long",
            })
            .Build();
        return new JwtTokenService(configuration);
    }

    private static JwtSecurityToken Decode(string token) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token);

    private static (Guid MembershipId, Guid OrgId, string Roles) DecodeMembershipClaim(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return (
            root.GetProperty("id").GetGuid(),
            root.GetProperty("organizationId").GetGuid(),
            root.GetProperty("roles").GetString()!);
    }

    private static string GenerateToken(
        JwtTokenService service,
        IEnumerable<MembershipSummary>? memberships = null,
        string? activeMembershipId = null) =>
        service.GenerateAccessToken(
            userId: UserId.From(Guid.NewGuid()),
            userType: UserType.Provider,
            email: Email.Create("owner@asanrezerve.test"),
            displayName: "Test Owner",
            firstName: "Test",
            lastName: "Owner",
            status: "Active",
            roles: new[] { "Provider" },
            memberships: memberships,
            activeMembershipId: activeMembershipId);

    [Fact]
    public void No_memberships_supplied_emits_no_membership_claims()
    {
        var token = GenerateToken(CreateService());

        var jwt = Decode(token);

        jwt.Claims.Where(c => c.Type == "membership").Should().BeEmpty();
        jwt.Claims.Where(c => c.Type == "activeMembershipId").Should().BeEmpty();
    }

    [Fact]
    public void Emits_one_membership_claim_per_membership_with_the_correct_fields()
    {
        var membershipId1 = Guid.NewGuid();
        var orgId1 = Guid.NewGuid();
        var membershipId2 = Guid.NewGuid();
        var orgId2 = Guid.NewGuid();

        var memberships = new[]
        {
            new MembershipSummary(membershipId1, orgId1, "Owner,StaffProvider", "Active"),
            new MembershipSummary(membershipId2, orgId2, "StaffProvider", "Active"),
        };

        var token = GenerateToken(CreateService(), memberships);
        var jwt = Decode(token);

        var membershipClaims = jwt.Claims.Where(c => c.Type == "membership").ToList();
        membershipClaims.Should().HaveCount(2);

        var decoded = membershipClaims.Select(c => DecodeMembershipClaim(c.Value)).ToList();
        decoded.Should().ContainSingle(m => m.MembershipId == membershipId1 && m.OrgId == orgId1
            && m.Roles == "Owner,StaffProvider");
        decoded.Should().ContainSingle(m => m.MembershipId == membershipId2 && m.OrgId == orgId2
            && m.Roles == "StaffProvider");
    }

    [Fact]
    public void Emits_the_activeMembershipId_claim_when_supplied()
    {
        var activeId = Guid.NewGuid().ToString();
        var memberships = new[] { new MembershipSummary(Guid.Parse(activeId), Guid.NewGuid(), "Owner", "Active") };

        var token = GenerateToken(CreateService(), memberships, activeId);
        var jwt = Decode(token);

        jwt.Claims.Single(c => c.Type == "activeMembershipId").Value.Should().Be(activeId);
    }

    [Fact]
    public void A_null_activeMembershipId_emits_no_activeMembershipId_claim()
    {
        var memberships = new[] { new MembershipSummary(Guid.NewGuid(), Guid.NewGuid(), "Owner", "Active") };

        var token = GenerateToken(CreateService(), memberships, activeMembershipId: null);
        var jwt = Decode(token);

        jwt.Claims.Where(c => c.Type == "activeMembershipId").Should().BeEmpty();
    }
}
