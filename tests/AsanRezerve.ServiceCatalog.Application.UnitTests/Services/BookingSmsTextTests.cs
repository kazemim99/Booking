using AsanRezerve.ServiceCatalog.Application.Services.Notifications;
using FluentAssertions;

namespace AsanRezerve.ServiceCatalog.Application.UnitTests.Services;

/// <summary>
/// The SMS a customer gets when the salon books them
/// (openspec/changes/_inline/walk-in-customer-name-sms). It must read as a person wrote it: their
/// name, the salon, the service, and the date in the calendar they use.
/// </summary>
public class BookingSmsTextTests
{
    [Fact]
    public void The_date_is_the_persian_one_the_customer_reads()
    {
        // 2026-09-21 is Monday 30 Shahrivar 1405. Written the way people say it (QA walkthrough 2026-09-22): a
        // slash date in Latin digits is scrambled by right-to-left text and read back as "۳ هفته ۴ ساعت ۵ جمعه".
        BookingSmsText.PersianDate(new DateTime(2026, 9, 21, 9, 0, 0))
            .Should().Be("دوشنبه ۳۰ شهریور");
    }

    [Fact]
    public void The_time_is_written_in_persian_digits()
    {
        BookingSmsText.PersianTime(new DateTime(2026, 9, 21, 9, 5, 0)).Should().Be("۰۹:۰۵");
    }

    [Fact]
    public void The_message_names_the_customer_the_salon_the_service_and_the_time()
    {
        var message = BookingSmsText.Confirmed(
            "مرتضی", "سالن نهال", "احیای مو", new DateTime(2026, 9, 21, 9, 0, 0));

        message.Should().Be("مرتضی عزیز، نوبت شما در سالن نهال برای احیای مو دوشنبه ۳۰ شهریور، ساعت ۰۹:۰۰ ثبت شد.");
    }

    [Fact]
    public void The_time_is_the_salons_wall_clock_never_shifted()
    {
        // The slot the salon picked at 18:30 must read 18:30, whatever the server's timezone
        // (booking times carry no zone — FOLLOW-UPS #63).
        BookingSmsText.Confirmed("سارا", "سالن نهال", "کوتاهی", new DateTime(2026, 9, 21, 18, 30, 0))
            .Should().Contain("ساعت ۱۸:۳۰");
    }

    [Fact]
    public void A_customer_with_no_name_is_still_addressed_politely()
    {
        BookingSmsText.Confirmed("  ", "سالن نهال", "", new DateTime(2026, 9, 21, 9, 0, 0))
            .Should().StartWith("مشتری گرامی عزیز، نوبت شما در سالن نهال دوشنبه");
    }
}
