using AsanRezerve.Host.Composition;
using AsanRezerve.ServiceCatalog.Application.Services.Interfaces;
using AsanRezerve.UserManagement.Domain.Enums;
using AsanRezerve.UserManagement.Domain.Repositories;
using AsanRezerve.Core.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AsanRezerve.Host.CompositionTests;

/// <summary>
/// Locks down how the Host satisfies ServiceCatalog's
/// <see cref="IPersonAccountProvisioningService"/> port — the third loopback retired this way
/// (after <see cref="ProviderInfoCompositionTests"/> and <see cref="TokenServiceCompositionTests"/>).
///
/// <para>Unlike those two, there is no HTTP adapter competing for this registration to guard
/// against (this interface's only registration is the in-process one) — what matters here is
/// that the type resolves at all (proving <see cref="AsanRezerve.UserManagement.Domain.Services.IPersonProvisioningService"/>
/// and <see cref="IUserRepository"/> are both reachable from the same container ServiceCatalog's
/// handler runs in) and that create + the compensating delete actually do what they claim
/// against a real database, not just that the DI graph is shaped correctly.</para>
/// </summary>
[Collection(HostCompositionCollection.Name)]
public sealed class PersonAccountProvisioningCompositionTests
{
    private readonly HostCompositionFactory _factory;

    public PersonAccountProvisioningCompositionTests(HostCompositionFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void The_Host_Resolves_The_InProcess_Adapter()
    {
        using var scope = _factory.Services.CreateScope();

        var resolved = scope.ServiceProvider.GetRequiredService<IPersonAccountProvisioningService>();

        resolved.Should().BeOfType<InProcessPersonAccountProvisioningService>();
    }

    [Fact]
    public void The_Adapter_Is_Scoped()
    {
        using var first = _factory.Services.CreateScope();
        using var second = _factory.Services.CreateScope();

        var a = first.ServiceProvider.GetRequiredService<IPersonAccountProvisioningService>();
        var b = second.ServiceProvider.GetRequiredService<IPersonAccountProvisioningService>();

        a.Should().NotBeSameAs(b, "a captured instance would outlive the DbContext its repository depends on");
    }

    /// <summary>
    /// Proves the create half reaches UserManagement's real, guarded account-creation path —
    /// not just that a Guid comes back, but that a person genuinely exists afterward.
    /// </summary>
    [Fact]
    public async Task CreateWithPhoneAsync_Persists_A_Real_Findable_Person()
    {
        using var scope = _factory.Services.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IPersonAccountProvisioningService>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var phone = $"+9891{Random.Shared.Next(10000000, 99999999)}";
        var created = await sut.CreateWithPhoneAsync(phone, "Test", "Invitee", null, CancellationToken.None);

        var person = await userRepository.GetByIdAsync(UserId.From(created.PersonId), CancellationToken.None);
        person.Should().NotBeNull("the id returned must correspond to a real, persisted user");
        person!.Status.Should().NotBe(UserStatus.Deleted);
        created.IsNewAccount.Should().BeTrue("nobody had this phone before the call");
    }

    /// <summary>
    /// The flag a caller's compensation keys on: a second call for a phone that already has an
    /// account must report the SAME person and IsNewAccount = false. Register-and-accept used
    /// to assume "true" from its own earlier lookup, and under a race the loser soft-deleted the
    /// winner's account.
    /// </summary>
    [Fact]
    public async Task CreateWithPhoneAsync_For_A_Phone_That_Already_Has_An_Account_Reuses_It_And_Says_So()
    {
        using var scope = _factory.Services.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IPersonAccountProvisioningService>();

        var phone = $"+9891{Random.Shared.Next(10000000, 99999999)}";
        var first = await sut.CreateWithPhoneAsync(phone, "First", "Caller", null, CancellationToken.None);
        var second = await sut.CreateWithPhoneAsync(phone, "Second", "Caller", null, CancellationToken.None);

        second.PersonId.Should().Be(first.PersonId, "one person per phone");
        second.IsNewAccount.Should().BeFalse("the account already existed, so this call must not claim to own it");
    }

    /// <summary>
    /// Proves the compensating delete is not the silent no-op it used to be: this is the exact
    /// scenario that was broken (SysAdminOnly-gated, and no handler for DeleteUserCommand at
    /// all) -- create, then compensate, then confirm the account is genuinely gone (soft-deleted)
    /// and no longer blocks the same phone from registering again.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_Actually_Removes_The_Account_And_Frees_The_Phone_Number()
    {
        using var scope = _factory.Services.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IPersonAccountProvisioningService>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var phone = $"+9891{Random.Shared.Next(10000000, 99999999)}";
        var created = await sut.CreateWithPhoneAsync(phone, "Test", "Orphan", null, CancellationToken.None);

        var deleted = await sut.DeleteAsync(created.PersonId, "compensating for a failed register-and-accept",
            CancellationToken.None);

        deleted.Should().BeTrue("the account genuinely exists and must be removable");

        var stillExists = await userRepository.ExistsByPhoneNumberAsync(
            PhoneNumber.From(phone), CancellationToken.None);
        stillExists.Should().BeFalse(
            "a Deleted-status row must be excluded from the uniqueness check, or the phone number " +
            "stays permanently claimed by an account that never got past the failed sign-up");
    }

    [Fact]
    public async Task DeleteAsync_Reports_False_Rather_Than_Throwing_For_An_Unknown_Person()
    {
        using var scope = _factory.Services.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IPersonAccountProvisioningService>();

        var deleted = await sut.DeleteAsync(Guid.NewGuid(), "no such person", CancellationToken.None);

        deleted.Should().BeFalse();
    }

    // A provider who signs in by phone gets a placeholder name («ارائه‌دهنده 9123135143»), and
    // onboarding stored the real owner name only on the salon — so the staff picker listed the owner
    // under the placeholder (2026-09-19). The onboarding name replaces a placeholder, never a real name.
    [Fact]
    public async Task AdoptNameIfPlaceholder_Replaces_The_Phone_Sign_In_Placeholder()
    {
        using var scope = _factory.Services.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IPersonAccountProvisioningService>();
        var phone = $"+9891{Random.Shared.Next(10000000, 99999999)}";
        var created = await sut.CreateWithPhoneAsync(phone, null, null, null, CancellationToken.None);

        var changed = await sut.AdoptNameIfPlaceholderAsync(created.PersonId, "مصطفی", "کاظمی", CancellationToken.None);

        changed.Should().BeTrue();
        using var readScope = _factory.Services.CreateScope();
        var person = await readScope.ServiceProvider.GetRequiredService<IUserRepository>()
            .GetByIdAsync(UserId.From(created.PersonId), CancellationToken.None);
        person!.Profile.FirstName.Should().Be("مصطفی");
        person.Profile.LastName.Should().Be("کاظمی");
    }

    [Fact]
    public async Task AdoptNameIfPlaceholder_Never_Overwrites_A_Name_The_Person_Has()
    {
        using var scope = _factory.Services.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IPersonAccountProvisioningService>();
        var phone = $"+9891{Random.Shared.Next(10000000, 99999999)}";
        var created = await sut.CreateWithPhoneAsync(phone, "مرتضی", "کاظمی", null, CancellationToken.None);

        var changed = await sut.AdoptNameIfPlaceholderAsync(created.PersonId, "نام", "دیگر", CancellationToken.None);

        changed.Should().BeFalse();
        using var readScope = _factory.Services.CreateScope();
        var person = await readScope.ServiceProvider.GetRequiredService<IUserRepository>()
            .GetByIdAsync(UserId.From(created.PersonId), CancellationToken.None);
        person!.Profile.FirstName.Should().Be("مرتضی");
    }
}
