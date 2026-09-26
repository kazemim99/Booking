using AsanRezerve.Core.Application.Behaviors;
using AsanRezerve.Core.Application.Exceptions;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AsanRezerve.ServiceCatalog.Application.UnitTests.Behaviors;

/// <summary>
/// MediatR requests are logged at Debug, destructured so the masking step can see inside them, and a failure is
/// logged once (system-logging).
/// <para><c>LoggingBehavior</c> used to write every command as a JSON string at Information — passwords, OTP codes
/// and refresh tokens included, and a string cannot be masked — and to log every failure as an Error that the
/// HTTP exception handler then logged again.</para>
/// </summary>
public sealed class LoggingBehaviorTests
{
    public sealed record ChangePassword(string CurrentPassword, string NewPassword) : IRequest<string>;

    private sealed class RecordingLogger : ILogger<LoggingBehavior<ChangePassword, string>>
    {
        public List<(LogLevel Level, Exception? Exception, IReadOnlyList<KeyValuePair<string, object?>> State)> Entries { get; } = [];

        public LogLevel Minimum { get; init; } = LogLevel.Trace;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= Minimum;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            Entries.Add((logLevel, exception, state as IReadOnlyList<KeyValuePair<string, object?>> ?? []));
        }
    }

    private static IHttpContextAccessor InRequest() =>
        new HttpContextAccessor { HttpContext = new DefaultHttpContext() };

    private static IHttpContextAccessor InBackgroundJob() => new HttpContextAccessor();

    private static readonly ChangePassword Request = new("old-secret", "new-secret");

    [Fact]
    public async Task A_successful_request_writes_nothing_at_information()
    {
        var log = new RecordingLogger();

        await new LoggingBehavior<ChangePassword, string>(log, InRequest())
            .Handle(Request, _ => Task.FromResult("ok"), CancellationToken.None);

        log.Entries.Should().OnlyContain(e => e.Level == LogLevel.Debug);
    }

    [Fact]
    public async Task The_request_is_destructured_not_serialised_so_it_can_be_masked()
    {
        var log = new RecordingLogger();

        await new LoggingBehavior<ChangePassword, string>(log, InRequest())
            .Handle(Request, _ => Task.FromResult("ok"), CancellationToken.None);

        var payload = log.Entries.SelectMany(e => e.State).Where(kv => kv.Key == "@Request").Select(kv => kv.Value);
        payload.Should().ContainSingle().Which.Should().BeSameAs(Request);
    }

    [Fact]
    public async Task Nothing_is_built_when_debug_is_off()
    {
        var log = new RecordingLogger { Minimum = LogLevel.Information };

        await new LoggingBehavior<ChangePassword, string>(log, InRequest())
            .Handle(Request, _ => Task.FromResult("ok"), CancellationToken.None);

        log.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task A_failure_during_an_http_request_is_left_to_the_exception_handler()
    {
        var log = new RecordingLogger();
        var boom = new InvalidOperationException("db down");

        var act = () => new LoggingBehavior<ChangePassword, string>(log, InRequest())
            .Handle(Request, _ => throw boom, CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(boom);
        log.Entries.Should().NotContain(e => e.Level >= LogLevel.Warning, "the HTTP edge logs it once, with the request's status");
    }

    [Fact]
    public async Task A_failure_in_a_background_job_is_an_error_with_its_stack_trace()
    {
        var log = new RecordingLogger();
        var boom = new InvalidOperationException("db down");

        var act = () => new LoggingBehavior<ChangePassword, string>(log, InBackgroundJob())
            .Handle(Request, _ => throw boom, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        log.Entries.Where(e => e.Level >= LogLevel.Warning).Should().ContainSingle()
            .Which.Should().Match<(LogLevel Level, Exception? Exception, IReadOnlyList<KeyValuePair<string, object?>> State)>(
                e => e.Level == LogLevel.Error && e.Exception == boom);
    }

    [Fact]
    public async Task An_expected_rejection_in_a_background_job_is_a_warning_without_a_stack_trace()
    {
        var log = new RecordingLogger();

        var act = () => new LoggingBehavior<ChangePassword, string>(log, InBackgroundJob())
            .Handle(Request, _ => throw new NotFoundException("Booking", Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        var entry = log.Entries.Where(e => e.Level >= LogLevel.Warning).Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Warning);
        entry.Exception.Should().BeNull();
    }
}
