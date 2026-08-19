using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.ServiceCatalog.Infrastructure.Services.Application;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.Unit;

/// <summary>
/// Regression cover for provider registration's final step failing with
/// <c>HTTP 500: Token generation failed with status Unauthorized</c> — and for a second, independent defect on
/// the same code path that only became visible once the first was fixed and the call actually started
/// returning data.
///
/// <para><b>Defect 1 — no credentials on the internal call.</b> <see cref="TokenService"/> calls UserManagement's
/// <c>POST /api/v1/auth/generate-token</c> over HTTP — a loopback call within the same single-host modular
/// monolith, not a call to a separate service. That endpoint has no <c>[AllowAnonymous]</c>, so it falls under
/// the host's global <c>FallbackPolicy.RequireAuthenticatedUser()</c>. The <c>UserManagementAPI</c> HTTP client
/// only attaches an <c>X-API-Key</c> header when <c>Services:UserManagement:ApiKey</c> is configured — it is
/// empty by default — and no API-key authentication scheme is registered on the receiving side regardless, only
/// JWT Bearer. So the call went out with no credentials at all and always 401'd.
/// <b>Fix:</b> re-present the current request's OWN bearer token. This method only ever runs inside an
/// already-authenticated request, and <c>GenerateToken</c> does not check the caller matches the target user
/// id, so forwarding the caller's own token is sufficient and grants nothing beyond what that caller already
/// had.</para>
///
/// <para><b>Defect 2 — the response envelope was never unwrapped.</b> Every controller response on this host is
/// wrapped by <c>ApiResponseMiddleware</c> into <c>{ success, statusCode, message, data, metadata }</c> — this
/// is true of every endpoint in the whole API, verified against a live call. The actual token fields live at
/// <c>.data</c>, but <see cref="TokenService"/> deserialized the response body straight into
/// <see cref="TokenResponse"/>, which matches nothing at the envelope's root. System.Text.Json does not throw
/// for unmatched properties — it just leaves the record at its defaults — so this "succeeded" with a non-null
/// <see cref="TokenResponse"/> whose <c>AccessToken</c> was always <c>string.Empty</c>. Provider registration's
/// final step therefore returned <c>HTTP 200</c> with a blank <c>accessToken</c>: registration genuinely
/// succeeded, but the provider never received the fresh, provider-claim-bearing session the call exists to
/// produce. <b>Fix:</b> deserialize into a small envelope type and read its <c>.Data</c>; additionally guard on
/// an empty <c>AccessToken</c> so a similarly-shaped failure throws instead of returning quietly-wrong data.</para>
///
/// <para>An earlier version of both this fix and this test file misdiagnosed defect 2 as a camelCase/PascalCase
/// casing mismatch. That was wrong — this project's <c>ReadFromJsonAsync</c> calls already bind camelCase JSON
/// to PascalCase properties without extra options — and was only caught by replaying the real request against
/// the running host and reading the actual response bytes rather than trusting the theory. The fake HTTP
/// response below is therefore built as the REAL envelope shape (captured from that live call), not a bare
/// <see cref="TokenResponse"/> — using the bare shape, as the first version of this file did, would pass
/// regardless of whether the unwrapping fix is correct.</para>
/// </summary>
public class TokenServiceTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public HttpStatusCode StatusToReturn { get; set; } = HttpStatusCode.OK;

        public TokenResponse ResponseData { get; set; } = new()
        {
            AccessToken = "new-token",
            RefreshToken = "refresh",
            ExpiresIn = 900,
        };

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;

            var response = new HttpResponseMessage(StatusToReturn);
            if (StatusToReturn == HttpStatusCode.OK)
            {
                // The real ApiResponseMiddleware envelope, matching a live capture of
                // POST /api/v1/auth/generate-token:
                //   { "success": true, "statusCode": 200, "message": "...", "data": {...}, "metadata": {...} }
                response.Content = JsonContent.Create(new
                {
                    success = true,
                    statusCode = 200,
                    message = "Request completed successfully",
                    data = ResponseData,
                    metadata = new { requestId = "test", timestamp = DateTime.UtcNow },
                });
            }

            return Task.FromResult(response);
        }
    }

    private static (TokenService service, CapturingHandler handler) BuildService(string? inboundAuthHeader)
    {
        var handler = new CapturingHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("UserManagementAPI")).Returns(httpClient);

        HttpContext? httpContext = null;
        if (inboundAuthHeader != null)
        {
            var ctx = new DefaultHttpContext();
            ctx.Request.Headers.Authorization = inboundAuthHeader;
            httpContext = ctx;
        }

        var contextAccessor = new Mock<IHttpContextAccessor>();
        contextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        var service = new TokenService(factory.Object, contextAccessor.Object, NullLogger<TokenService>.Instance);
        return (service, handler);
    }

    [Fact]
    public async Task Forwards_the_caller_own_bearer_token_to_the_internal_call()
    {
        var (service, handler) = BuildService(inboundAuthHeader: "Bearer caller-own-token");

        await service.GenerateTokenWithProviderClaimsAsync(Guid.NewGuid(), Guid.NewGuid(), "Active");

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Headers.Authorization.Should().NotBeNull(
            "the internal call must authenticate as the caller, not go out unauthenticated");
        handler.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.LastRequest.Headers.Authorization.Parameter.Should().Be("caller-own-token");
    }

    [Fact]
    public async Task Succeeds_end_to_end_and_unwraps_the_real_response_envelope()
    {
        var (service, _) = BuildService(inboundAuthHeader: "Bearer caller-own-token");

        var result = await service.GenerateTokenWithProviderClaimsAsync(
            Guid.NewGuid(), Guid.NewGuid(), "Active");

        // Only passes if TokenService reads .data rather than the envelope root -- the fake response above has
        // no top-level accessToken/refreshToken/expiresIn at all, exactly like the real host's response.
        result.AccessToken.Should().Be("new-token");
        result.RefreshToken.Should().Be("refresh");
        result.ExpiresIn.Should().Be(900);
    }

    [Fact]
    public async Task Reproduces_the_original_defect_when_there_is_no_inbound_token_to_forward()
    {
        // No Authorization header on the inbound HttpContext (e.g. no ambient request, or the caller genuinely
        // was not authenticated) — nothing exists to forward, so the outbound call goes out with no credentials,
        // which is exactly the pre-fix behaviour and exactly what the server's FallbackPolicy rejects.
        var (service, handler) = BuildService(inboundAuthHeader: null);
        handler.StatusToReturn = HttpStatusCode.Unauthorized;

        var act = () => service.GenerateTokenWithProviderClaimsAsync(Guid.NewGuid(), Guid.NewGuid(), "Active");

        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*Unauthorized*",
                "this is the exact HTTP 500 'Token generation failed with status Unauthorized' the provider hit");
        handler.LastRequest!.Headers.Authorization.Should().BeNull(
            "with no inbound token there is nothing to forward; the call must not fabricate one");
    }

    [Fact]
    public async Task Throws_rather_than_silently_returning_a_blank_access_token()
    {
        // Pins the belt-and-braces guard added alongside the envelope-unwrapping fix: even a well-formed,
        // successful (HTTP 200) response must be rejected if AccessToken comes back empty, rather than handed
        // back to the caller as if it were a real token. This is exactly what used to happen silently when the
        // envelope was never unwrapped -- a non-null TokenResponse with every field at its default -- and the
        // guard now catches that shape regardless of what causes it.
        var (service, handler) = BuildService(inboundAuthHeader: "Bearer caller-own-token");
        handler.ResponseData = new TokenResponse { AccessToken = "", RefreshToken = "refresh", ExpiresIn = 900 };

        var act = () => service.GenerateTokenWithProviderClaimsAsync(Guid.NewGuid(), Guid.NewGuid(), "Active");

        await act.Should().ThrowAsync<InvalidOperationException>(
            "registration completion must fail loudly rather than report success with no usable token");
    }

    [Fact]
    public async Task Reproduces_defect_2_when_the_envelope_is_not_unwrapped()
    {
        // Directly pins the root cause: reading the SAME real-shaped envelope response straight into
        // TokenResponse (the pre-fix approach) matches nothing at the root and silently yields empty defaults
        // rather than throwing. This is what "HTTP 200 with a blank accessToken" actually was.
        var handler = new CapturingHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };

        var response = await httpClient.PostAsync("api/v1/auth/generate-token", content: null);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var misreadAsIfUnwrapped = await response.Content.ReadFromJsonAsync<TokenResponse>(options);

        misreadAsIfUnwrapped.Should().NotBeNull(
            "System.Text.Json does not fail on unmatched properties -- this is the silent-default trap itself");
        misreadAsIfUnwrapped!.AccessToken.Should().BeEmpty(
            "reading the envelope root as if it were the payload yields exactly the empty token the provider hit");
    }
}
