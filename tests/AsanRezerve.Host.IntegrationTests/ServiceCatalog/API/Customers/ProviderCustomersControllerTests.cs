using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.Customers;

/// <summary>
/// A salon's own customer book (openspec/changes/provider-customer-book): customers the provider adds
/// by hand or imports from the contacts they tick, so booking a regular is a tap, not typing.
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class ProviderCustomersControllerTests : ServiceCatalogIntegrationTestBase
{
    public ProviderCustomersControllerTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    private static string Url(Guid providerId, string suffix = "") => $"/api/v1/providers/{providerId}/customers{suffix}";

    private static async Task<JToken> Data(HttpResponseMessage response) =>
        JObject.Parse(await response.Content.ReadAsStringAsync())["data"]!;

    private async Task<JArray> List(Guid providerId, string? search = null)
    {
        var response = await Client.GetAsync(Url(providerId, search is null ? "" : $"?search={Uri.EscapeDataString(search)}"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (JArray)await Data(response);
    }

    [Fact]
    public async Task K1_An_added_customer_is_listed_with_a_normalized_phone()
    {
        var provider = await CreateAndAuthenticateAsProviderAsync();

        var response = await Client.PostAsJsonAsync(Url(provider.Id.Value),
            new { firstName = "مرتضی", lastName = "کاظمی", phoneNumber = "0912 313 5143", notes = "مشتری ثابت" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var list = await List(provider.Id.Value);
        list.Should().ContainSingle();
        list[0]["firstName"]!.Value<string>().Should().Be("مرتضی");
        list[0]["lastName"]!.Value<string>().Should().Be("کاظمی");
        list[0]["phoneNumber"]!.Value<string>().Should().Be("+989123135143", "stored normalized, whatever was typed");
        list[0]["source"]!.Value<string>().Should().Be("Manual");
    }

    [Fact]
    public async Task K2_The_same_phone_is_one_customer_per_salon_whatever_its_spelling()
    {
        var provider = await CreateAndAuthenticateAsProviderAsync();
        await Client.PostAsJsonAsync(Url(provider.Id.Value), new { firstName = "مرتضی", lastName = "کاظمی", phoneNumber = "09123135143" });

        var again = await Client.PostAsJsonAsync(Url(provider.Id.Value),
            new { firstName = "کسی", lastName = "دیگر", phoneNumber = "+98 912 313 5143" });

        again.StatusCode.Should().Be(HttpStatusCode.Conflict, "adding never silently overwrites a saved name");
        (await List(provider.Id.Value)).Should().ContainSingle();
    }

    [Fact]
    public async Task K1_Search_finds_by_name_or_by_part_of_the_number()
    {
        var provider = await CreateAndAuthenticateAsProviderAsync();
        await Client.PostAsJsonAsync(Url(provider.Id.Value), new { firstName = "مرتضی", lastName = "کاظمی", phoneNumber = "09123135143" });
        await Client.PostAsJsonAsync(Url(provider.Id.Value), new { firstName = "سارا", lastName = "احمدی", phoneNumber = "09351112233" });

        (await List(provider.Id.Value, "کاظمی")).Should().ContainSingle().Which["firstName"]!.Value<string>().Should().Be("مرتضی");
        (await List(provider.Id.Value, "1112233")).Should().ContainSingle().Which["firstName"]!.Value<string>().Should().Be("سارا");
    }

    [Fact]
    public async Task K4_A_customer_can_be_edited_and_removed()
    {
        var provider = await CreateAndAuthenticateAsProviderAsync();
        var created = await Client.PostAsJsonAsync(Url(provider.Id.Value), new { firstName = "مرتضی", lastName = "کاظمی", phoneNumber = "09123135143" });
        var id = (await Data(created))["id"]!.Value<string>();

        var edit = await Client.PutAsJsonAsync(Url(provider.Id.Value, $"/{id}"),
            new { firstName = "مرتضی", lastName = "کاظمی‌نژاد", phoneNumber = "09123135143", notes = "VIP" });
        edit.IsSuccessStatusCode.Should().BeTrue();
        (await List(provider.Id.Value))[0]["lastName"]!.Value<string>().Should().Be("کاظمی‌نژاد");

        var remove = await Client.DeleteAsync(Url(provider.Id.Value, $"/{id}"));
        remove.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await List(provider.Id.Value)).Should().BeEmpty();
    }

    [Fact]
    public async Task K3_Import_adds_new_contacts_skips_saved_ones_and_reports_bad_numbers()
    {
        var provider = await CreateAndAuthenticateAsProviderAsync();
        await Client.PostAsJsonAsync(Url(provider.Id.Value), new { firstName = "مرتضی", lastName = "کاظمی", phoneNumber = "09123135143" });

        var response = await Client.PostAsJsonAsync(Url(provider.Id.Value, "/import"), new
        {
            customers = new object[]
            {
                new { firstName = "سارا", lastName = "احمدی", phoneNumber = "09351112233" },  // new
                new { firstName = "مرتضی", lastName = "ک", phoneNumber = "+989123135143" },  // already saved
                new { firstName = "غلط", lastName = "", phoneNumber = "123" },                // not a phone
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await Data(response);
        result["added"]!.Value<int>().Should().Be(1);
        result["alreadySaved"]!.Value<int>().Should().Be(1);
        result["invalid"]!.Value<int>().Should().Be(1);
        var list = await List(provider.Id.Value);
        list.Should().HaveCount(2);
        list.Single(c => c["firstName"]!.Value<string>() == "مرتضی")["lastName"]!.Value<string>()
            .Should().Be("کاظمی", "import never overwrites a name the salon saved");
        list.Single(c => c["firstName"]!.Value<string>() == "سارا")["source"]!.Value<string>().Should().Be("Contacts");
    }

    [Fact]
    public async Task A_customer_needs_a_first_name_and_a_valid_phone()
    {
        var provider = await CreateAndAuthenticateAsProviderAsync();

        (await Client.PostAsJsonAsync(Url(provider.Id.Value), new { firstName = "", lastName = "کاظمی", phoneNumber = "09123135143" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Client.PostAsJsonAsync(Url(provider.Id.Value), new { firstName = "مرتضی", lastName = "کاظمی", phoneNumber = "12" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task K7_Another_salon_can_neither_read_nor_change_them()
    {
        var mine = await CreateAndAuthenticateAsProviderAsync("Mine", "mine@test.com");
        await Client.PostAsJsonAsync(Url(mine.Id.Value), new { firstName = "مرتضی", lastName = "کاظمی", phoneNumber = "09123135143" });

        await CreateAndAuthenticateAsProviderAsync("Theirs", "theirs@test.com");

        (await Client.GetAsync(Url(mine.Id.Value))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Client.PostAsJsonAsync(Url(mine.Id.Value), new { firstName = "x", lastName = "y", phoneNumber = "09351112233" }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static DateTime NextWeekdayAtHour(DateTime from, int hour)
    {
        var day = from.Date;
        while (day.DayOfWeek is System.DayOfWeek.Saturday or System.DayOfWeek.Sunday)
            day = day.AddDays(1);
        return day.AddHours(hour);
    }

    private async Task<HttpResponseMessage> BookFor(Guid providerId, Guid serviceId, object? providerCustomerId) =>
        await Client.PostAsJsonAsync("/api/v1/bookings", new
        {
            providerId,
            serviceId,
            staffProviderId = providerId,
            startTime = NextWeekdayAtHour(DateTime.UtcNow.Date.AddDays(2), 10),
            customerNotes = "مشتری حضوری",
            providerCustomerId,
        });

    [Fact]
    public async Task K5_A_booking_made_for_a_saved_customer_is_in_that_customers_history()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        AuthenticateAsProviderOwner(provider);

        var created = await Client.PostAsJsonAsync(Url(provider.Id.Value),
            new { firstName = "مرتضی", lastName = "کاظمی", phoneNumber = "09123135143" });
        var customerId = (string)(await Data(created))["id"]!;

        var booking = await BookFor(provider.Id.Value, service.Id.Value, customerId);
        booking.StatusCode.Should().Be(HttpStatusCode.Created, await booking.Content.ReadAsStringAsync());

        var listed = (await List(provider.Id.Value)).Single();
        ((int)listed["totalBookings"]!).Should().Be(1);
        ((int)listed["upcomingBookings"]!).Should().Be(1);
        listed["lastBookingAt"]!.Type.Should().NotBe(JTokenType.Null);
    }

    private async Task<HttpResponseMessage> BookWalkIn(
        Guid providerId, Guid serviceId, object? firstName, object? lastName, object? phone) =>
        await Client.PostAsJsonAsync("/api/v1/bookings", new
        {
            providerId,
            serviceId,
            staffProviderId = providerId,
            startTime = NextWeekdayAtHour(DateTime.UtcNow.Date.AddDays(2), 10),
            walkInFirstName = firstName,
            walkInLastName = lastName,
            walkInPhone = phone,
        });

    [Fact]
    public async Task A_customer_typed_on_the_booking_screen_joins_the_book_and_is_booked_for()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        AuthenticateAsProviderOwner(provider);

        var booking = await BookWalkIn(
            provider.Id.Value, service.Id.Value, "مرتضی", "کاظمی", "0912 313 5143");
        booking.StatusCode.Should().Be(HttpStatusCode.Created, await booking.Content.ReadAsStringAsync());

        var listed = (await List(provider.Id.Value)).Single();
        listed["firstName"]!.Value<string>().Should().Be("مرتضی");
        listed["phoneNumber"]!.Value<string>().Should().Be("+989123135143");
        listed["source"]!.Value<string>().Should().Be("Booking");
        ((int)listed["totalBookings"]!).Should().Be(1, "the booking is recorded for them");
    }

    [Fact]
    public async Task A_number_already_in_the_book_is_booked_for_that_customer_not_added_again()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        AuthenticateAsProviderOwner(provider);
        await Client.PostAsJsonAsync(Url(provider.Id.Value),
            new { firstName = "مرتضی", lastName = "کاظمی", phoneNumber = "09123135143" });

        var booking = await BookWalkIn(
            provider.Id.Value, service.Id.Value, "مرتضي", "ک", "+98 912 313 5143");
        booking.StatusCode.Should().Be(HttpStatusCode.Created, await booking.Content.ReadAsStringAsync());

        var list = await List(provider.Id.Value);
        list.Should().ContainSingle("the same number is one customer, whatever its spelling");
        list[0]["lastName"]!.Value<string>().Should().Be("کاظمی", "a saved name is never overwritten");
        ((int)list[0]["totalBookings"]!).Should().Be(1);
    }

    [Fact]
    public async Task A_salon_booking_needs_the_customers_name_and_number()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        AuthenticateAsProviderOwner(provider);

        (await BookWalkIn(provider.Id.Value, service.Id.Value, null, null, null))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await BookWalkIn(provider.Id.Value, service.Id.Value, "مرتضی", "کاظمی", null))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await BookWalkIn(provider.Id.Value, service.Id.Value, "مرتضی", "کاظمی", "12"))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await BookWalkIn(provider.Id.Value, service.Id.Value, "", "", "09123135143"))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await List(provider.Id.Value)).Should().BeEmpty("a refused booking saves nobody");
    }

    [Fact]
    public async Task A_customer_booking_online_needs_no_walk_in_details()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        AuthenticateAsUser(Guid.NewGuid(), "customer@test.com");

        var booking = await BookWalkIn(
            provider.Id.Value, service.Id.Value, null, null, null);

        booking.StatusCode.Should().Be(HttpStatusCode.Created, await booking.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task K5_A_booking_cannot_name_another_salons_customer()
    {
        var theirs = await CreateAndAuthenticateAsProviderAsync("Theirs", "theirs@test.com");
        var created = await Client.PostAsJsonAsync(Url(theirs.Id.Value),
            new { firstName = "مرتضی", lastName = "کاظمی", phoneNumber = "09123135143" });
        var theirCustomer = (string)(await Data(created))["id"]!;

        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        AuthenticateAsProviderOwner(provider);

        var booking = await BookFor(provider.Id.Value, service.Id.Value, theirCustomer);
        booking.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
