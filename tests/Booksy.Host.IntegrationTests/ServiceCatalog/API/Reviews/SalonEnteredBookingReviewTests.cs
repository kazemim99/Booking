using System.Net;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Reviews;

/// <summary>
/// A booking the salon entered for someone's mobile number is that person's to review
/// (openspec/changes/_inline/customer-reviews-and-nahal-seed). Such a booking stores the salon's OWNER as its
/// customer; the person it is for is the salon's client-book entry with that number, and sign-in proves the number
/// (the rule customer-sees-salon-bookings already applies to «نوبت‌های من»). Before: the person saw the booking in
/// their list, pressed «ثبت نظر», and got an empty 403 — while the owner, as its nominal customer, could review their
/// own salon.
/// </summary>
[Collection(BooksyHostTestCollection.Name)]
public class SalonEnteredBookingReviewTests : ReviewTestBase
{
    public SalonEnteredBookingReviewTests(BooksyHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task The_person_with_the_booked_number_can_review_it()
    {
        var phone = NewPhone();
        var visit = await SalonEnteredCompletedVisitAsync(phone);
        var person = await SignUpAsync(phone, "مرتضی", "کاظمی");

        var reviewId = await ReviewedAsync(visit with { CustomerId = person });

        var review = await LoadReviewAsync(reviewId);
        review.CustomerId.Value.Should().Be(person, "the review is theirs, not the salon owner's");
        review.ProviderId.Should().Be(visit.Provider.Id);
    }

    [Fact]
    public async Task Their_booking_list_and_booking_page_offer_it()
    {
        var phone = NewPhone();
        var visit = await SalonEnteredCompletedVisitAsync(phone);
        var person = await SignUpAsync(phone, "مرتضی", "کاظمی");

        (await MyBookingRowAsync(person, visit.BookingId))["canReview"]!.Value<bool>().Should().BeTrue();

        var page = await BookingPageAsync(person, visit.BookingId);
        var text = await page.Content.ReadAsStringAsync();
        page.StatusCode.Should().Be(HttpStatusCode.OK, text);
        Data(text)["canReview"]!.Value<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task The_salon_owner_cannot_review_their_own_salon_through_it()
    {
        var visit = await SalonEnteredCompletedVisitAsync(NewPhone());

        var response = await SubmitAsync(visit, AReview());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task The_owners_own_booking_list_never_offers_to_review_their_salon()
    {
        var visit = await SalonEnteredCompletedVisitAsync(NewPhone());

        // The owner's «my bookings» lists the walk-ins they entered (stored under their id); it must not then offer
        // them «ثبت نظر» and refuse it.
        var row = await MyBookingRowAsync(visit.CustomerId, visit.BookingId);

        row["canReview"]!.Value<bool>().Should().BeFalse();
    }

    [Fact]
    public async Task Somebody_with_another_number_cannot_review_or_open_it()
    {
        var visit = await SalonEnteredCompletedVisitAsync(NewPhone());
        var stranger = await SignUpAsync(NewPhone(), "سارا", "احمدی");

        var review = await SubmitAsync(visit with { CustomerId = stranger }, AReview());
        review.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await BookingPageAsync(stranger, visit.BookingId)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
