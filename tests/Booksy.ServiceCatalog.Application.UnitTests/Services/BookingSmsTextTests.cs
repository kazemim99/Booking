using Booksy.ServiceCatalog.Application.Services.Notifications;
using FluentAssertions;

namespace Booksy.ServiceCatalog.Application.UnitTests.Services;

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
        // 2026-09-21 is Monday 1405/06/30.
        BookingSmsText.PersianDate(new DateTime(2026, 9, 21, 9, 0, 0))
            .Should().Be("دوشنبه 1405/06/30");
    }

    [Fact]
    public void The_message_names_the_customer_the_salon_the_service_and_the_time()
    {
        var message = BookingSmsText.Confirmed(
            "مرتضی", "سالن نهال", "احیای مو", new DateTime(2026, 9, 21, 9, 0, 0));

        message.Should().Be("مرتضی عزیز، نوبت شما در سالن نهال برای احیای مو دوشنبه 1405/06/30 ساعت 09:00 ثبت شد.");
    }

    [Fact]
    public void The_time_is_the_salons_wall_clock_never_shifted()
    {
        // The slot the salon picked at 18:30 must read 18:30, whatever the server's timezone
        // (booking times carry no zone — FOLLOW-UPS #63).
        BookingSmsText.Confirmed("سارا", "سالن نهال", "کوتاهی", new DateTime(2026, 9, 21, 18, 30, 0))
            .Should().Contain("ساعت 18:30");
    }

    [Fact]
    public void A_customer_with_no_name_is_still_addressed_politely()
    {
        BookingSmsText.Confirmed("  ", "سالن نهال", "", new DateTime(2026, 9, 21, 9, 0, 0))
            .Should().StartWith("مشتری گرامی عزیز، نوبت شما در سالن نهال دوشنبه");
    }
}
