using System.Diagnostics;
using System.Text.Json;
using AsanRezerve.API.Middleware;
using AsanRezerve.Core.Application.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AsanRezerve.ServiceCatalog.Api.UnitTests.Middleware;

/// <summary>
/// An exception that ends a request is logged once, at a level matching its outcome (system-logging).
/// <para>One failing command used to write up to five Error events with the same stack trace, and a rejected form
/// (400) was an Error like a crash — the real faults drowned in expected ones.</para>
/// </summary>
public class ExceptionLoggingTests
{
    private sealed class RecordingLogger : ILogger<ExceptionHandlingMiddleware>
    {
        public List<(LogLevel Level, Exception? Exception, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, exception, formatter(state, exception)));
    }

    private static async Task<(RecordingLogger Log, HttpContext Context)> Fail(Exception exception)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Production");
        var log = new RecordingLogger();
        var middleware = new ExceptionHandlingMiddleware(_ => throw exception, log, environment);

        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/v1/bookings";
        context.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(context);
        return (log, context);
    }

    [Fact]
    public async Task A_server_fault_is_one_error_with_its_stack_trace()
    {
        var boom = new InvalidOperationException("db down");

        var (log, context) = await Fail(boom);

        context.Response.StatusCode.Should().Be(500);
        log.Entries.Should().ContainSingle().Which.Should().Match<(LogLevel Level, Exception? Exception, string Message)>(
            e => e.Level == LogLevel.Error && e.Exception == boom);
    }

    [Fact]
    public async Task A_client_error_is_not_an_error_and_carries_no_stack_trace()
    {
        var (log, context) = await Fail(new NotFoundException("Booking", Guid.NewGuid()));

        context.Response.StatusCode.Should().Be(404);
        var entry = log.Entries.Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Information);
        entry.Exception.Should().BeNull();
        entry.Message.Should().Contain("404");
    }

    [Fact]
    public async Task The_error_body_carries_the_trace_id_to_quote_to_support()
    {
        using var activity = new Activity("request").SetIdFormat(ActivityIdFormat.W3C).Start();

        var (_, context) = await Fail(new InvalidOperationException("boom"));

        context.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(context.Response.Body);
        body.RootElement.GetProperty("metadata").GetProperty("traceId").GetString()
            .Should().Be(activity.TraceId.ToHexString());
    }
}
