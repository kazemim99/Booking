using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Policies;

namespace Booksy.ServiceCatalog.Domain.UnitTests.Policies;

/// <summary>
/// The catalogue's own guard rails.
/// </summary>
/// <remarks>
/// These are not tests of behaviour so much as a standing review of the product's notification set: they
/// fail when someone adds a notification without saying what it is, gives a routine message an SMS, or
/// quietly widens the set of things a person is not allowed to switch off.
/// </remarks>
public class NotificationEventCatalogTests
{
    private static IEnumerable<NotificationEventCode> RealCodes =>
        Enum.GetValues<NotificationEventCode>().Where(c => c != NotificationEventCode.None);

    [Fact]
    public void Every_code_has_an_entry()
    {
        var missing = RealCodes
            .Where(c => !NotificationEventCatalog.TryDescribe(c, out _))
            .ToList();

        Assert.True(
            missing.Count == 0,
            "Every notification must state what it is. Missing catalogue entries: " +
            string.Join(", ", missing));
    }

    [Fact]
    public void The_catalogue_describes_nothing_that_is_not_a_code()
    {
        // The other direction: an entry for a code that no longer exists is dead weight that reads as
        // coverage.
        var orphans = NotificationEventCatalog.AllCodes
            .Where(c => !Enum.IsDefined(c))
            .ToList();

        Assert.True(orphans.Count == 0, "Catalogue entries for undefined codes: " + string.Join(", ", orphans));
    }

    [Fact]
    public void None_is_never_describable()
    {
        // default(NotificationEventCode) must not resolve to something sendable.
        Assert.False(NotificationEventCatalog.TryDescribe(NotificationEventCode.None, out _));
    }

    [Fact]
    public void Only_critical_notifications_may_cost_an_sms()
    {
        // Each SMS is billed. A routine message that acquired one should show up here, not on an invoice.
        var offenders = NotificationEventCatalog.AllCodes
            .Select(c => (Code: c, Descriptor: NotificationEventCatalog.Describe(c)))
            .Where(x => x.Descriptor.Channels.HasFlag(NotificationChannel.SMS)
                        && x.Descriptor.Criticality != NotificationCriticality.Critical)
            .Select(x => x.Code)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "SMS is reserved for critical notifications (user decision, 2026-09-19). Offenders: " +
            string.Join(", ", offenders));
    }

    [Fact]
    public void Every_notification_has_somewhere_to_go()
    {
        var channelless = NotificationEventCatalog.AllCodes
            .Where(c => NotificationEventCatalog.Describe(c).Channels == NotificationChannel.None)
            .ToList();

        Assert.True(
            channelless.Count == 0,
            "A notification with no channel can never reach anyone: " + string.Join(", ", channelless));
    }

    [Fact]
    public void Suppressibility_is_exactly_the_inverse_of_criticality()
    {
        foreach (var code in NotificationEventCatalog.AllCodes)
        {
            var descriptor = NotificationEventCatalog.Describe(code);
            Assert.Equal(
                descriptor.Criticality == NotificationCriticality.Standard,
                descriptor.IsSuppressible);
        }
    }

    [Fact]
    public void The_critical_set_is_exactly_what_product_agreed()
    {
        // A change to this list is a product decision about what a person may not switch off, so it should
        // be a deliberate edit to this test and not a side effect of adding a notification.
        var expected = new[]
        {
            NotificationEventCode.BookingConfirmed,
            NotificationEventCode.BookingRejected,
            NotificationEventCode.BookingRescheduled,
            NotificationEventCode.BookingCancelledByProvider,
            NotificationEventCode.BookingReminder2h,
            NotificationEventCode.PaymentReceived,
            NotificationEventCode.PaymentFailed,
            NotificationEventCode.RefundProcessed,
            NotificationEventCode.PhoneVerification,
            NotificationEventCode.PasswordReset,
            NotificationEventCode.SecurityAlert,
            NotificationEventCode.NewBookingRequest,
            NotificationEventCode.BookingCancelledByCustomer,
            NotificationEventCode.InvitationSent,
            NotificationEventCode.PayoutCompleted,
            NotificationEventCode.ProviderActivated,
        };

        var actual = NotificationEventCatalog.AllCodes
            .Where(c => NotificationEventCatalog.Describe(c).Criticality == NotificationCriticality.Critical)
            .OrderBy(c => c.ToString(), StringComparer.Ordinal)
            .ToList();

        Assert.Equal(
            expected.OrderBy(c => c.ToString(), StringComparer.Ordinal).ToList(),
            actual);
    }

    [Fact]
    public void Describing_an_uncatalogued_code_fails_loudly()
    {
        // Silently defaulting would send a notification nobody specified, on channels nobody chose.
        var uncatalogued = (NotificationEventCode)9999;

        var ex = Assert.Throws<KeyNotFoundException>(() => NotificationEventCatalog.Describe(uncatalogued));
        Assert.Contains("catalogue entry", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(NotificationEventCode.BookingConfirmed, NotificationAudience.Customer)]
    [InlineData(NotificationEventCode.NewBookingRequest, NotificationAudience.Provider)]
    [InlineData(NotificationEventCode.StaffAssignedToBooking, NotificationAudience.StaffMember)]
    public void A_notification_addresses_the_party_who_can_act_on_it(
        NotificationEventCode code,
        NotificationAudience expected)
    {
        Assert.Equal(expected, NotificationEventCatalog.Describe(code).Audience);
    }

    [Fact]
    public void Cancellation_tells_the_party_who_did_not_cancel()
    {
        // The whole reason these are two codes rather than one.
        Assert.Equal(
            NotificationAudience.Customer,
            NotificationEventCatalog.Describe(NotificationEventCode.BookingCancelledByProvider).Audience);

        Assert.Equal(
            NotificationAudience.Provider,
            NotificationEventCatalog.Describe(NotificationEventCode.BookingCancelledByCustomer).Audience);
    }
}
