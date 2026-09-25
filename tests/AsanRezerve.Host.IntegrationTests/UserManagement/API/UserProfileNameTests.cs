using System.Net;
using System.Net.Http.Json;
using AsanRezerve.UserManagement.API.Models.Requests;
using FluentAssertions;

namespace AsanRezerve.UserManagement.IntegrationTests.API;

/// <summary>
/// A person naming themselves (openspec/changes/_inline/walk-in-customer-name-sms). Phone sign-in
/// gives every account a placeholder name («ارائه‌دهنده 9123135143»), which is what colleagues and
/// customers then see; PUT /users/{id}/profile had no handler at all (FOLLOW-UPS #64), so there was
/// no way to fix it. The phone is the sign-in identity and is NOT changed here.
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class UserProfileNameTests : UserManagementIntegrationTestBase
{
    public UserProfileNameTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task A_person_replaces_the_placeholder_name_their_sign_in_gave_them()
    {
        var customer = await CreateAndAuthenticateAsCustomerAsync(
            "ارائه‌دهنده", "9123135143", "+989123135143");

        var response = await Client.PutAsJsonAsync(
            $"/api/v1/users/{customer.UserId.Value}/profile",
            new UpdateUserProfileRequest { FirstName = "مصطفی", LastName = "کاظمی" });

        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        var user = await FindUserAsync(customer.UserId.Value);
        user!.Profile.FirstName.Should().Be("مصطفی");
        user.Profile.LastName.Should().Be("کاظمی");
    }

    [Fact]
    public async Task An_empty_name_is_refused_so_nobody_ends_up_nameless()
    {
        var customer = await CreateAndAuthenticateAsCustomerAsync(
            "مصطفی", "کاظمی", "+989123135144");

        var response = await Client.PutAsJsonAsync(
            $"/api/v1/users/{customer.UserId.Value}/profile",
            new UpdateUserProfileRequest { FirstName = "   ", LastName = "کاظمی" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await FindUserAsync(customer.UserId.Value))!.Profile.FirstName.Should().Be("مصطفی");
    }

    [Fact]
    public async Task The_sign_in_number_cannot_be_changed_here()
    {
        var customer = await CreateAndAuthenticateAsCustomerAsync(
            "مصطفی", "کاظمی", "+989123135145");

        var response = await Client.PutAsJsonAsync(
            $"/api/v1/users/{customer.UserId.Value}/profile",
            new UpdateUserProfileRequest
            {
                FirstName = "مصطفی",
                LastName = "کاظمی",
                PhoneNumber = "+989350001122",
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "the phone is the sign-in identity; changing it has its own verified flow");
    }

    [Fact]
    public async Task Nobody_renames_somebody_else()
    {
        var victim = await CreateTestCustomerAsync("سارا", "احمدی", "+989351112233");
        await CreateAndAuthenticateAsCustomerAsync("مرتضی", "کاظمی", "+989123135146");

        var response = await Client.PutAsJsonAsync(
            $"/api/v1/users/{victim.UserId.Value}/profile",
            new UpdateUserProfileRequest { FirstName = "هرکسی", LastName = "دیگر" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await FindUserAsync(victim.UserId.Value))!.Profile.FirstName.Should().Be("سارا");
    }
}
