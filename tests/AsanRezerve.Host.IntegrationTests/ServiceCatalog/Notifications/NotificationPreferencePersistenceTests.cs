using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.UserNotificationPreferencesAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.Notifications;

/// <summary>
/// How a preference mask survives the database, which is what decides whether section 8 needs a data
/// migration at all.
/// </summary>
/// <remarks>
/// <para><c>EnabledTypes</c> is persisted with <c>HasConversion&lt;string&gt;()</c> — <b>by name, not as an
/// integer</b>. That is the single most important fact about repairing <see cref="NotificationType"/>:
/// renumbering the members cannot silently re-label existing rows the way it would if the column held a
/// bitmask, because the column holds words.</para>
///
/// <para>Characterisation, like its unit-test sibling: these record today's behaviour so the repair reads as
/// a diff. The test that would fail on a bad renumbering is the last one, and it fails loudly rather than by
/// quietly changing what a stored row means.</para>
/// </remarks>
[Collection(AsanRezerveHostTestCollection.Name)]
public class NotificationPreferencePersistenceTests : ServiceCatalogIntegrationTestBase
{
    public NotificationPreferencePersistenceTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task A_default_mask_survives_a_round_trip()
    {
        var userId = UserId.From(Guid.NewGuid());

        await SaveAsync(userId, NotificationPreferenceCategory.All);

        (await ReloadAsync(userId))!.Preferences.EnabledTypes.Should().Be(NotificationPreferenceCategory.All);
    }

    [Fact]
    public async Task A_multi_bit_mask_survives_a_round_trip()
    {
        // Before the section 8 split this test held All | ReviewRequest — the combination whose rendering
        // dropped a category the user had enabled and invented one they had not. That value cannot be built
        // any more: a preference category is a real bit, and the sequential members are not categories.
        var userId = UserId.From(Guid.NewGuid());
        var mask = NotificationPreferenceCategory.Promotions | NotificationPreferenceCategory.Newsletter;

        await SaveAsync(userId, mask);

        (await ReloadAsync(userId))!.Preferences.EnabledTypes.Should().Be(mask);
    }

    [Fact]
    public async Task A_single_category_survives_a_round_trip()
    {
        var userId = UserId.From(Guid.NewGuid());

        await SaveAsync(userId, NotificationPreferenceCategory.SecurityAlert);

        (await ReloadAsync(userId))!.Preferences.EnabledTypes.Should().Be(NotificationPreferenceCategory.SecurityAlert);
    }

    [Fact]
    public async Task The_column_holds_names_not_a_number()
    {
        // Asserted against the raw column, because this is the property the whole migration question turns
        // on. If this ever becomes an integer, renumbering the enum starts rewriting what existing rows
        // mean, and section 8 needs a data migration after all.
        var userId = UserId.From(Guid.NewGuid());
        await SaveAsync(userId, NotificationPreferenceCategory.All);

        var stored = await RawEnabledTypesAsync(userId);

        stored.Should().Be("All");
        int.TryParse(stored, out _).Should().BeFalse("the column is a name, not a bitmask");
    }

    [Fact]
    public async Task A_multi_type_mask_is_stored_as_its_decomposed_names()
    {
        var userId = UserId.From(Guid.NewGuid());
        await SaveAsync(userId, NotificationPreference.Minimal.EnabledTypes);

        var stored = await RawEnabledTypesAsync(userId);

        stored.Should().Be("BookingReminder, BookingConfirmation, PaymentReceived");
    }

    // ── arrange ──

    private async Task SaveAsync(UserId userId, NotificationPreferenceCategory types)
    {
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IUserNotificationPreferencesRepository>();
        var uow = scope.ServiceProvider
            .GetRequiredService<ServiceCatalog.Application.Abstractions.Persistence.IServiceCatalogUnitOfWork>();

        // The repository adds to the context and stops, like everything else here; whatever commits the
        // caller's work is what writes the row.
        await repo.SaveAsync(UserNotificationPreferences.Create(
            userId,
            NotificationPreference.Create(NotificationChannel.SMS | NotificationChannel.InApp, types)));

        await uow.CommitAsync();
    }

    private async Task<UserNotificationPreferences?> ReloadAsync(UserId userId)
    {
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IUserNotificationPreferencesRepository>();
        return await repo.GetByUserIdAsync(userId);
    }

    /// <summary>
    /// Reads the column itself, in SQL. Going through EF would hand back whatever the value converter
    /// decodes, which is precisely the layer under test.
    /// </summary>
    private async Task<string?> RawEnabledTypesAsync(UserId userId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();

        var id = userId.Value;
        return await db.Database
            .SqlQuery<string?>($@"SELECT ""EnabledTypes"" AS ""Value""
                                  FROM ""ServiceCatalog"".""UserNotificationPreferences""
                                  WHERE ""UserId"" = {id}")
            .FirstOrDefaultAsync();
    }
}
