using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Policies;
using FluentAssertions;

namespace Booksy.ServiceCatalog.Application.UnitTests.Notifications;

/// <summary>
/// What recipients actually read.
/// </summary>
/// <remarks>
/// The completeness test is the important one: it is what stops a notification reaching an Iranian customer
/// as an empty string or an English placeholder because someone added a code and forgot the words.
/// </remarks>
public class PersianNotificationCopyWriterTests
{
    private readonly PersianNotificationCopyWriter _writer = new();

    /// <summary>A booking on Wednesday 2026-09-23 at 14:30, salon wall-clock (FOLLOW-UPS #63).</summary>
    private static readonly DateTime StartTime = new(2026, 9, 23, 14, 30, 0, DateTimeKind.Unspecified);

    private static Dictionary<string, string> FullParameters() => new()
    {
        [NotificationParameter.CustomerName] = "مریم",
        [NotificationParameter.BusinessName] = "سالن نهال",
        [NotificationParameter.ServiceName] = "کوتاهی مو",
        [NotificationParameter.StaffName] = "زهرا",
        [NotificationParameter.StartTime] = StartTime.ToString("o"),
        [NotificationParameter.Amount] = "۵۰۰٬۰۰۰ تومان",
        [NotificationParameter.Reason] = "دلیل نمونه",
        [NotificationParameter.Code] = "222222",
        [NotificationParameter.Count] = "۴",
    };

    [Fact]
    public void Every_catalogued_notification_has_wording()
    {
        var parameters = FullParameters();

        var missing = new List<string>();
        foreach (var code in NotificationEventCatalog.AllCodes)
        {
            try
            {
                var copy = _writer.Write(code, parameters);
                if (string.IsNullOrWhiteSpace(copy.Subject) || string.IsNullOrWhiteSpace(copy.Body))
                    missing.Add($"{code} (blank)");
            }
            catch (KeyNotFoundException)
            {
                missing.Add($"{code} (no entry)");
            }
        }

        missing.Should().BeEmpty("every notification a person can receive must have words");
    }

    [Fact]
    public void Wording_is_persian_not_english()
    {
        var parameters = FullParameters();

        foreach (var code in NotificationEventCatalog.AllCodes)
        {
            var copy = _writer.Write(code, parameters);

            // Any Arabic-script character. Catches an entry left as an English placeholder.
            copy.Body.Should().MatchRegex("[؀-ۿ]", $"{code} should read as Persian");
            copy.Subject.Should().MatchRegex("[؀-ۿ]", $"{code}'s title should read as Persian");
        }
    }

    [Fact]
    public void A_booking_time_is_rendered_jalali_with_the_salon_wall_clock()
    {
        var copy = _writer.Write(NotificationEventCode.BookingConfirmed, FullParameters());

        // 2026-09-23 is Wednesday 1 Mehr 1405.
        copy.Body.Should().Contain("چهارشنبه ۱ مهر، ساعت ۱۴:۳۰");
    }

    [Theory]
    [InlineData(NotificationEventCode.BookingConfirmed)]
    [InlineData(NotificationEventCode.BookingRejected)]
    [InlineData(NotificationEventCode.BookingCancelledByProvider)]
    [InlineData(NotificationEventCode.BookingRescheduled)]
    public void A_salon_decision_tells_the_customer_which_appointment_it_is_about(NotificationEventCode code)
    {
        // The customer did not act, so the notice is all they have to go on: which salon, which service, which
        // day and which wall-clock time (QA recording 2026-09-23 — «نوبت شما در سالن نهال برای … ساعت … تأیید شد»).
        var copy = _writer.Write(code, FullParameters());

        copy.Body.Should().StartWith("مریم عزیز،");
        copy.Body.Should().Contain("سالن نهال");
        copy.Body.Should().Contain("کوتاهی مو", $"{code} should name the service");
        copy.Body.Should().Contain("چهارشنبه ۱ مهر، ساعت ۱۴:۳۰");
    }

    [Theory]
    [InlineData(NotificationEventCode.NewBookingRequest)]
    [InlineData(NotificationEventCode.NewBookingConfirmed)]
    [InlineData(NotificationEventCode.BookingCancelledByCustomer)]
    public void A_salon_notice_without_a_customer_name_says_a_customer_not_dear_customer(NotificationEventCode code)
    {
        // «درخواست نوبت جدید از مشتری گرامی» addresses the salon as if it were the customer (QA 2026-09-22).
        var parameters = FullParameters();
        parameters.Remove(NotificationParameter.CustomerName);

        var copy = _writer.Write(code, parameters);

        copy.Body.Should().Contain("یک مشتری");
        copy.Body.Should().NotContain("مشتری گرامی");
    }

    [Fact]
    public void A_salon_notice_names_the_customer_when_it_has_the_name()
    {
        var parameters = FullParameters();
        parameters[NotificationParameter.CustomerName] = "سارا احمدی";

        _writer.Write(NotificationEventCode.NewBookingRequest, parameters).Body.Should().Contain("سارا احمدی");
    }

    [Fact]
    public void The_sms_text_and_the_body_say_the_same_thing()
    {
        // Two wordings for one notification is how a customer ends up being told different things depending
        // on which channel reached them first.
        foreach (var code in NotificationEventCatalog.AllCodes)
        {
            var copy = _writer.Write(code, FullParameters());
            copy.PlainTextBody.Should().Be(copy.Body, $"{code}");
        }
    }

    [Fact]
    public void A_missing_customer_name_falls_back_to_a_polite_form()
    {
        var copy = _writer.Write(
            NotificationEventCode.BookingConfirmed,
            new Dictionary<string, string> { [NotificationParameter.BusinessName] = "سالن نهال" });

        copy.Body.Should().Contain("مشتری گرامی");
    }

    [Fact]
    public void A_missing_time_never_prints_a_wrong_date()
    {
        // Falling back to DateTime.MinValue would tell the customer their appointment is in the year 622.
        var copy = _writer.Write(
            NotificationEventCode.BookingConfirmed,
            new Dictionary<string, string> { [NotificationParameter.CustomerName] = "مریم" });

        copy.Body.Should().Contain("در زمان تعیین‌شده");
        copy.Body.Should().NotContain("0001");
        copy.Body.Should().NotContain("1348");
    }

    [Fact]
    public void An_unparseable_time_is_treated_as_missing_rather_than_guessed()
    {
        var copy = _writer.Write(
            NotificationEventCode.BookingConfirmed,
            new Dictionary<string, string> { [NotificationParameter.StartTime] = "not a date" });

        copy.Body.Should().Contain("در زمان تعیین‌شده");
    }

    [Fact]
    public void Cancellation_wording_names_who_cancelled()
    {
        var parameters = FullParameters();

        var toCustomer = _writer.Write(NotificationEventCode.BookingCancelledByProvider, parameters);
        var toProvider = _writer.Write(NotificationEventCode.BookingCancelledByCustomer, parameters);

        toCustomer.Body.Should().Contain("سالن");
        toCustomer.Body.Should().NotBe(toProvider.Body);
    }

    [Fact]
    public void A_verification_code_appears_in_the_message()
    {
        var copy = _writer.Write(
            NotificationEventCode.PhoneVerification,
            new Dictionary<string, string> { [NotificationParameter.Code] = "123456" });

        copy.Body.Should().Contain("123456");
    }

    [Fact]
    public void An_uncatalogued_notification_fails_loudly()
    {
        var act = () => _writer.Write((NotificationEventCode)9999, FullParameters());

        act.Should().Throw<KeyNotFoundException>().WithMessage("*no wording*");
    }
}
