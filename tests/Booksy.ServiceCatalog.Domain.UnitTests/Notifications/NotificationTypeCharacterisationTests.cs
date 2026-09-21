using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.UserNotificationPreferencesAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Policies;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.UnitTests.Notifications;

/// <summary>
/// What <see cref="NotificationType"/> does today — including the parts that are wrong.
/// </summary>
/// <remarks>
/// <para>This file is a characterisation, not a specification. Every assertion here records BEHAVIOUR AS IT
/// IS on 2026-09-21, so that the repair in tasks.md section 8 can be read as a diff: a test that changes is
/// a behaviour that changed, deliberately. Several of the assertions below pin outcomes that are defects.
/// They are labelled as such and must not be read as the intended design.</para>
///
/// <para><b>The defect.</b> The enum is declared <c>[Flags]</c>, but its members from 16777216 up are
/// sequential integers rather than distinct bits — <c>ReviewRequest</c> is bit 24, and everything after it
/// is bit 24 plus a low bit that already belongs to a booking or payment type. So bitwise membership, which
/// is the only thing <c>[Flags]</c> means, reports nonsense between them.</para>
///
/// <para><b>How far it reaches, measured rather than assumed.</b> Nothing in the live dispatch path consults
/// <c>EnabledTypes</c>: <c>NotificationSuppressionPolicy.ShouldSend</c> asks only about channels, and the one
/// method that does test type membership — <c>UserNotificationPreferences.ShouldSendNotification</c> — has no
/// callers, as do the aggregate's <c>EnableTypes</c>/<c>DisableTypes</c>. So no notification is currently
/// sent or withheld because of this. What IS live is the settings screen, which renders the stored mask by
/// name, and that is where the defect surfaces today (see the last two tests).</para>
/// </remarks>
public class NotificationTypeCharacterisationTests
{
    // ── The numbering, which is the root of everything below ──

    [Fact]
    public void All_is_the_low_24_bits_and_ReviewRequest_is_the_25th()
    {
        Assert.Equal(16777215, (int)NotificationType.All);
        Assert.Equal(16777216, (int)NotificationType.ReviewRequest);
    }

    [Fact]
    public void Everything_after_ReviewRequest_counts_rather_than_shifts()
    {
        // This is the defect in one line: consecutive integers in a [Flags] enum. RefundProcessed is
        // ReviewRequest's bit plus NewBooking's bit, because 16777217 = 16777216 + 1.
        Assert.Equal((int)NotificationType.ReviewRequest + 1, (int)NotificationType.RefundProcessed);
        Assert.Equal((int)NotificationType.ReviewRequest + 2, (int)NotificationType.BookingCancellation);
        Assert.Equal((int)NotificationType.ReviewRequest + 16, (int)NotificationType.AccountDeactivated);
    }

    // ── DEFECT: bitwise membership between the sequential members ──

    [Fact]
    public void DEFECT_a_refund_issued_claims_to_contain_a_refund_processed()
    {
        // 16777223 & 16777217 == 16777217. Two unrelated notifications, one "containing" the other.
        Assert.True(NotificationType.RefundIssued.HasFlag(NotificationType.RefundProcessed));
    }

    [Fact]
    public void DEFECT_a_no_show_claims_to_contain_a_refund_and_a_cancellation()
    {
        Assert.True(NotificationType.BookingNoShow.HasFlag(NotificationType.RefundProcessed));
        Assert.True(NotificationType.BookingNoShow.HasFlag(NotificationType.BookingCancellation));
    }

    [Fact]
    public void DEFECT_every_sequential_member_claims_to_contain_ReviewRequest()
    {
        // They all carry bit 24, which IS ReviewRequest. So a preference mask holding any one of them reads
        // as having review requests enabled.
        Assert.True(NotificationType.PasswordReset.HasFlag(NotificationType.ReviewRequest));
        Assert.True(NotificationType.PhoneVerification.HasFlag(NotificationType.ReviewRequest));
        Assert.True(NotificationType.InvoiceGenerated.HasFlag(NotificationType.ReviewRequest));
    }

    [Fact]
    public void DEFECT_All_does_not_actually_mean_all()
    {
        // `All` is the OR of the bit-valued members only; the 17 sequential ones were added after it and
        // never folded in. A user on the default preference has every one of them reading as disabled.
        Assert.False(NotificationType.All.HasFlag(NotificationType.ReviewRequest));
        Assert.False(NotificationType.All.HasFlag(NotificationType.PasswordReset));
        Assert.Equal(NotificationType.All, NotificationPreference.Default.EnabledTypes);
    }

    // ── What the live paths actually do ──

    [Fact]
    public void The_send_decision_does_not_consult_enabled_types_at_all()
    {
        // The bound on the blast radius, asserted rather than remembered. With EVERY type disabled, a
        // suppressible notification on an enabled channel is still sent — because ShouldSend only ever asks
        // about channels. Section 8 must not change this by accident; if it starts honouring types, that is
        // a behaviour change needing its own decision.
        var preferences = UserNotificationPreferences.Create(
            UserId.From(Guid.NewGuid()),
            NotificationPreference.Create(NotificationChannel.SMS, NotificationType.None));

        Assert.True(NotificationSuppressionPolicy.ShouldSend(
            preferences, NotificationChannel.SMS, NotificationType.NewBooking));
    }

    [Fact]
    public void A_disabled_channel_is_what_actually_stops_a_suppressible_notification()
    {
        var preferences = UserNotificationPreferences.Create(
            UserId.From(Guid.NewGuid()),
            NotificationPreference.Create(NotificationChannel.Email, NotificationType.All));

        Assert.False(NotificationSuppressionPolicy.ShouldSend(
            preferences, NotificationChannel.SMS, NotificationType.NewBooking));
    }

    [Fact]
    public void Suppressibility_is_decided_by_set_membership_not_by_bits()
    {
        // NonSuppressible is a HashSet precisely because HasFlag cannot be trusted here. RefundIssued and
        // RefundProcessed are both in it, so this passes either way; BookingNoShow is the one that would
        // flip if anybody rewrote the set as a mask, since it bitwise-"contains" RefundProcessed.
        Assert.False(NotificationSuppressionPolicy.IsSuppressible(NotificationType.RefundProcessed));
        Assert.True(NotificationSuppressionPolicy.IsSuppressible(NotificationType.BookingNoShow));
    }

    // ── DEFECT: what the settings screen shows, which is live today ──

    [Fact]
    public void DEFECT_rendering_a_mask_by_name_drops_a_type_the_user_enabled()
    {
        // GetUserPreferencesQueryHandler renders the mask with ToString().Split(','), and the stored column
        // is written the same way (HasConversion<string>).
        //
        // .NET decomposes a flags value greedily, largest first. AccountDeactivated is bit24|bit4, and bit4
        // IS BookingConfirmation — so once AccountDeactivated is chosen to cover bit 24, BookingConfirmation
        // is already covered and never named. The user turned booking confirmations ON and the screen says
        // they are OFF.
        var mask = NotificationType.All | NotificationType.ReviewRequest;
        var rendered = mask.ToString();

        Assert.DoesNotContain("BookingConfirmation", rendered);
        Assert.True(mask.HasFlag(NotificationType.BookingConfirmation), "it really is enabled");
    }

    [Fact]
    public void DEFECT_rendering_a_mask_by_name_invents_a_type_the_user_never_set()
    {
        // The other half of the same decomposition: a name appears that nobody asked for.
        var mask = NotificationType.All | NotificationType.ReviewRequest;

        Assert.Contains("AccountDeactivated", mask.ToString());
    }

    [Fact]
    public void The_stored_string_still_round_trips_to_the_same_value()
    {
        // The saving grace, and the reason section 8 may not need a data migration: however misleading the
        // NAMES are, the decomposition ORs back to the value it came from. Persistence is by name, so
        // renumbering the enum does not rewrite what is already stored — only what it decomposes into.
        var mask = NotificationType.All | NotificationType.ReviewRequest;

        var reparsed = (NotificationType)Enum.Parse(typeof(NotificationType), mask.ToString());

        Assert.Equal(mask, reparsed);
    }

    [Fact]
    public void A_single_sequential_member_round_trips_by_its_own_name()
    {
        foreach (var value in new[]
                 {
                     NotificationType.ReviewRequest,
                     NotificationType.RefundIssued,
                     NotificationType.PasswordReset,
                     NotificationType.AccountDeactivated,
                 })
        {
            Assert.Equal(value.ToString(), Enum.GetName(typeof(NotificationType), value));
            Assert.Equal(value, (NotificationType)Enum.Parse(typeof(NotificationType), value.ToString()));
        }
    }
}
