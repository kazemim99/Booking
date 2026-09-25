using System.Diagnostics;
using System.Security.Claims;
using AsanRezerve.Infrastructure.Observability.Diagnostics;
using AsanRezerve.Infrastructure.Observability.Logging;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Events;

namespace AsanRezerve.Infrastructure.Observability.UnitTests.Diagnostics;

/// <summary>
/// One event per HTTP request and its trace id on the response (system-logging: "Each HTTP request is logged once
/// with a trace id"). It replaced a middleware that wrote two events per request under a GUID nobody else ever saw.
/// </summary>
public sealed class RequestTelemetryMiddlewareTests : IDisposable
{
    private readonly LoggingHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    private RequestTelemetryMiddleware Middleware(RequestDelegate next) =>
        new(next,
            _harness.Factory.CreateLogger<RequestTelemetryMiddleware>(),
            _harness.Services.GetRequiredService<IOptionsMonitor<ObservabilityLoggingOptions>>(),
            TimeProvider.System);

    private static DefaultHttpContext Request(string method = "GET", string path = "/api/v1/providers/1")
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        var responseFeature = new StartableResponseFeature();
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        return context;
    }

    private static object? Prop(LogEvent e, string name) => ((ScalarValue)e.Properties[name]).Value;

    [Fact]
    public async Task A_request_writes_one_completion_event()
    {
        var context = Request();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-7")], "test"));
        context.Request.Headers["X-Forwarded-For"] = "5.6.7.8, 10.0.0.1";

        await Middleware(c =>
        {
            c.Response.StatusCode = 200;
            return Task.CompletedTask;
        }).InvokeAsync(context);

        var logEvent = _harness.Sink.Single();
        logEvent.Level.Should().Be(LogEventLevel.Information);
        Prop(logEvent, "RequestMethod").Should().Be("GET");
        Prop(logEvent, "RequestPath").Should().Be("/api/v1/providers/1");
        Prop(logEvent, "StatusCode").Should().Be(200);
        logEvent.Properties.Should().ContainKey("ElapsedMs");
        Prop(logEvent, "UserId").Should().Be("user-7");
        Prop(logEvent, "ClientIp").Should().Be("5.6.7.8");
    }

    [Fact]
    public async Task The_response_carries_the_trace_id_every_event_of_the_request_has()
    {
        using var activity = new Activity("request").SetIdFormat(ActivityIdFormat.W3C).Start();
        var context = Request();

        await Middleware(c => ((StartableResponseFeature)c.Features.Get<IHttpResponseFeature>()!).StartAsync()).InvokeAsync(context);

        context.Response.Headers[RequestTelemetryMiddleware.TraceIdHeader].ToString().Should().Be(activity.TraceId.ToHexString());
        _harness.Sink.Single().TraceId.Should().Be(activity.TraceId);
    }

    [Theory]
    [InlineData(500, 10, LogEventLevel.Error)]
    [InlineData(503, 10, LogEventLevel.Error)]
    [InlineData(404, 10, LogEventLevel.Information)]
    [InlineData(200, 10, LogEventLevel.Information)]
    public void Level_follows_the_outcome(int status, double elapsedMs, LogEventLevel expected)
    {
        var level = RequestTelemetryMiddleware.LevelOf("/api/v1/x", status, elapsedMs, slowThresholdMs: 1000);

        ((int)level).Should().Be((int)expected);
    }

    [Fact]
    public void A_slow_request_is_a_warning()
    {
        RequestTelemetryMiddleware.LevelOf("/api/v1/x", 200, 1500, slowThresholdMs: 1000).Should().Be(LogLevel.Warning);
    }

    [Fact]
    public async Task Health_probes_do_not_fill_the_log()
    {
        await Middleware(_ => Task.CompletedTask).InvokeAsync(Request(path: "/health/ready"));

        _harness.Sink.Events.Should().BeEmpty("probes run every few seconds; they are logged at Debug");
    }

    [Fact]
    public async Task An_exception_that_escapes_is_logged_as_500_and_rethrown()
    {
        var context = Request();

        var act = () => Middleware(_ => throw new InvalidOperationException("boom")).InvokeAsync(context);

        await act.Should().ThrowAsync<InvalidOperationException>();
        var logEvent = _harness.Sink.Single();
        logEvent.Level.Should().Be(LogEventLevel.Error);
        Prop(logEvent, "StatusCode").Should().Be(500);
    }

    /// <summary>DefaultHttpContext's response feature never runs OnStarting callbacks; this one does.</summary>
    private sealed class StartableResponseFeature : HttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> _starting = [];
        private bool _started;

        public override bool HasStarted => _started;

        public override void OnStarting(Func<object, Task> callback, object state) => _starting.Add((callback, state));

        public async Task StartAsync()
        {
            if (_started) return;
            _started = true;
            for (var i = _starting.Count - 1; i >= 0; i--)
                await _starting[i].Callback(_starting[i].State);
        }
    }
}
