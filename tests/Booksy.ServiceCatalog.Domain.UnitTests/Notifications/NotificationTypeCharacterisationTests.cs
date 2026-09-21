using System.Reflection;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.UserNotificationPreferencesAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Policies;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.UnitTests.Notifications;

/// <summary>
/// The two enums that used to be one, and the defects that conflation caused.
/// </summary>
/// <remarks>
/// <para>This file began as a pure characterisation of <see cref="NotificationType"/> before the repair in
/// tasks.md section 8. It is kept as the record of that repair: each test says what the behaviour is now and,
/// where it changed, what it used to be. A reader should be able to tell from here alone what was wrong and
/// what was done about it.</para>
///
/// <para><b>What was wrong.</b> One enum was doing two incompatible jobs. It was the discriminator persisted
/// on every notification and template row, so it grew a member per notification and eventually ran past 24
/// bits into consecutive integers — while still carrying <c>[Flags]</c>, because it was also the mask a
/// recipient's preferences were stored as.</para>
///
/// <para><b>What was done.</b> The mask moved to <see cref="NotificationPreferenceCategory"/>, a real flags
/// enum with the same names and values as the bit-valued half, so no stored preference needed rewriting.
/// <see cref="NotificationType"/> kept every name and number — it is persisted as text and a removed name is
/// an unreadable row — and simply stopped claiming to be flags.</para>
/// </remarks>
public class NotificationTypeCharacterisationTests
{
    // ── The numbering that caused it, unchanged and now harmless ──

    [Fact]
    public void Everything_after_ReviewRequest_still_counts_rather_than_shifts()
    {
        // Deliberately NOT repaired. These values are persisted as names on Notifications.Type and
        // NotificationTemplates.Type; renumbering them would be a data migration for no gain, because
        // nothing compares them bitwise any more.
        Assert.Equal((int)NotificationType.ReviewRequest + 1, (int)NotificationType.RefundProcessed);
        Assert.Equal((int)NotificationType.ReviewRequest + 16, (int)NotificationType.AccountDeactivated);
    }

    [Fact]
    public void NotificationType_no_longer_claims_to_be_a_flags_enum()
    {
        // The attribute was the lie. Removing it is what stops ToString() decomposing a value greedily,
        // which is where the user-visible defect lived.
        Assert.Null(typeof(NotificationType).GetCustomAttribute<FlagsAttribute>());
    }

    [Fact]
    public void The_bit_arithmetic_between_sequential_members_is_still_nonsense_and_that_is_fine()
    {
        // Worth stating plainly rather than implying it was fixed: HasFlag is bit arithmetic and does not
        // consult the attribute, so this is STILL true. What changed is that nothing performs bitwise
        // membership on this enum any more — see the next test, which is the assertion that matters.
        Assert.True(NotificationType.RefundIssued.HasFlag(NotificationType.RefundProcessed));
    }

    [Fact]
    public void Nothing_in_the_domain_tests_membership_of_a_NotificationType_bitwise()
    {
        // The real guard. Suppressibility is set membership, and the only HasFlag left on the preference
        // path operates on NotificationPreferenceCategory, where every member is a distinct bit.
        Assert.False(NotificationSuppressionPolicy.IsSuppressible(NotificationType.RefundProcessed));
        Assert.True(NotificationSuppressionPolicy.IsSuppressible(NotificationType.BookingNoShow));

        // BookingNoShow bitwise-"contains" RefundProcessed, which is non-suppressible. If suppressibility
        // were ever rewritten as a mask, this pair is what would flip.
        Assert.True(NotificationType.BookingNoShow.HasFlag(NotificationType.RefundProcessed));
    }

    // ── The preference mask, now a genuine flags enum ──

    [Fact]
    public void All_now_actually_means_all()
    {
        // WAS: NotificationType.All was the OR of the bit members only, and the 17 that came after it were
        // never folded in — so a user on the default preference had every one of them reading as disabled.
        // Now there is nothing outside the bits to leave out.
        foreach (NotificationPreferenceCategory value in
                 Enum.GetValues<NotificationPreferenceCategory>())
        {
            Assert.True(
                NotificationPreferenceCategory.All.HasFlag(value),
                $"All should contain {value}");
        }
    }

    [Fact]
    public void Every_preference_category_is_a_distinct_bit()
    {
        // The invariant that makes a mask trustworthy, asserted rather than eyeballed. A future member added
        // as "the next integer" instead of "the next bit" fails here.
        var members = Enum.GetValues<NotificationPreferenceCategory>()
            .Where(v => v != NotificationPreferenceCategory.None
                        && v != NotificationPreferenceCategory.All)
            .ToList();

        foreach (var member in members)
        {
            var value = (int)member;
            Assert.True(
                value > 0 && (value & (value - 1)) == 0,
                $"{member} = {value} is not a single bit");
        }

        Assert.Equal(members.Count, members.Distinct().Count());
    }

    [Fact]
    public void The_preference_categories_kept_the_names_and_values_they_were_stored_under()
    {
        // This is what makes the split free of a data migration: preferences are persisted as text, so every
        // stored row still parses to the set it always meant. Changing a name here later is a migration,
        // not a rename.
        Assert.Equal(16777215, (int)NotificationPreferenceCategory.All);
        Assert.Equal(8, (int)NotificationPreferenceCategory.BookingReminder);
        Assert.Equal(16, (int)NotificationPreferenceCategory.BookingConfirmation);
        Assert.Equal(32, (int)NotificationPreferenceCategory.PaymentReceived);
        Assert.Equal(NotificationPreferenceCategory.All, NotificationPreference.Default.EnabledTypes);
    }

    // ── The live, user-visible defect, and its repair ──

    [Fact]
    public void Rendering_a_mask_by_name_no_longer_drops_or_invents_a_category()
    {
        // WAS THE DEFECT: the settings screen renders the mask with ToString().Split(','), and .NET
        // decomposes a flags value greedily, largest first. AccountDeactivated was bit24|bit4 and bit4 IS
        // BookingConfirmation, so for a mask of All-plus-ReviewRequest the rendering omitted
        // BookingConfirmation — which was enabled — and named AccountDeactivated, which was never set.
        //
        // That combination cannot be built now: the sequential members are not preference categories, so
        // there is no bit 24 to collide with, and All renders as itself.
        Assert.Equal("All", NotificationPreferenceCategory.All.ToString());

        var minimal = NotificationPreference.Minimal.EnabledTypes;
        var rendered = minimal.ToString();

        Assert.Contains("BookingConfirmation", rendered);
        Assert.DoesNotContain("AccountDeactivated", rendered);
    }

    [Fact]
    public void A_rendered_mask_still_round_trips_to_the_same_value()
    {
        foreach (var mask in new[]
                 {
                     NotificationPreferenceCategory.All,
                     NotificationPreference.Minimal.EnabledTypes,
                     NotificationPreferenceCategory.Promotions | NotificationPreferenceCategory.Newsletter,
                 })
        {
            var reparsed = Enum.Parse<NotificationPreferenceCategory>(mask.ToString());
            Assert.Equal(mask, reparsed);
        }
    }

    [Fact]
    public void A_stored_type_name_still_round_trips()
    {
        // Notifications.Type and NotificationTemplates.Type are text. Every name that was ever written has
        // to keep parsing, which is why none were removed.
        foreach (var value in new[]
                 {
                     NotificationType.ReviewRequest,
                     NotificationType.RefundIssued,
                     NotificationType.PasswordReset,
                     NotificationType.AccountDeactivated,
                     NotificationType.BookingReminder,
                 })
        {
            Assert.Equal(value.ToString(), Enum.GetName(value));
            Assert.Equal(value, Enum.Parse<NotificationType>(value.ToString()));
        }
    }

    // ── What the live send path does, unchanged by the split ──

    [Fact]
    public void The_send_decision_still_does_not_consult_enabled_types_at_all()
    {
        // Pinned before the repair and re-pinned after it: with EVERY category disabled, a suppressible
        // notification on an enabled channel is still sent, because ShouldSend only asks about channels.
        // The split was not meant to start honouring types, and this is the test that would catch it having
        // done so by accident.
        var preferences = UserNotificationPreferences.Create(
            UserId.From(Guid.NewGuid()),
            NotificationPreference.Create(NotificationChannel.SMS, NotificationPreferenceCategory.None));

        Assert.True(NotificationSuppressionPolicy.ShouldSend(
            preferences, NotificationChannel.SMS, NotificationType.NewBooking));
    }

    [Fact]
    public void A_disabled_channel_is_still_what_stops_a_suppressible_notification()
    {
        var preferences = UserNotificationPreferences.Create(
            UserId.From(Guid.NewGuid()),
            NotificationPreference.Create(NotificationChannel.Email, NotificationPreferenceCategory.All));

        Assert.False(NotificationSuppressionPolicy.ShouldSend(
            preferences, NotificationChannel.SMS, NotificationType.NewBooking));
    }

    [Fact]
    public void Every_catalogued_notification_still_has_a_stored_type()
    {
        // The mapping was renamed from PreferenceCategoryFor to NotificationTypeFor because its result is
        // written to Notifications.Type and was never consulted about anybody's preferences. Renaming it is
        // half the point of this section: the old name is what made the conflation look deliberate.
        foreach (var code in Enum.GetValues<NotificationEventCode>())
        {
            if (code == NotificationEventCode.None)
                continue;

            var type = NotificationEventCatalog.NotificationTypeFor(code);
            Assert.NotEqual(NotificationType.None, type);
        }
    }
}
