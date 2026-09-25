using AsanRezerve.API.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace AsanRezerve.ServiceCatalog.Api.UnitTests.Middleware;

/// <summary>
/// <see cref="UnauthorizedAccessException"/> has two unrelated meanings in this codebase. Handlers throw it on
/// purpose for "no authenticated user" (401), but the runtime ALSO throws it for a file-system permission error
/// (with an <see cref="IOException"/> inside). Production on 2026-09-19: <c>/app/wwwroot</c> was not writable by the
/// container user, every <c>/Providers/*</c> call failed creating the uploads folder, and the middleware answered
/// 401 — so the provider app refreshed its token, retried, got 401 again and logged the user out. A server fault
/// must answer 500, never an authentication status a client will act on.
/// </summary>
public class ExceptionHandlingMiddlewareStatusTests
{
    private static async Task<int> StatusFor(Exception exception)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Production");
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw exception,
            NullLogger<ExceptionHandlingMiddleware>.Instance,
            environment);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(context);
        return context.Response.StatusCode;
    }

    [Fact]
    public async Task A_handler_saying_no_authenticated_user_is_401()
    {
        (await StatusFor(new UnauthorizedAccessException("User not authenticated"))).Should().Be(401);
    }

    [Fact]
    public async Task A_file_system_permission_error_is_a_server_fault_not_401()
    {
        var ioDenied = new UnauthorizedAccessException(
            "Access to the path '/app/wwwroot/uploads' is denied.",
            new IOException("Permission denied"));

        (await StatusFor(ioDenied)).Should().Be(500,
            "a 401 makes clients refresh tokens and sign the user out over a server misconfiguration");
    }
}
