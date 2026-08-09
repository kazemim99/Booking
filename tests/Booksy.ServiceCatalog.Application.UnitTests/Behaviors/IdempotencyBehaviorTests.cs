using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Idempotency;
using Booksy.Core.Application.Behaviors;
using Booksy.Core.Application.Exceptions;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Booksy.ServiceCatalog.Application.UnitTests.Behaviors;

/// <summary>
/// C2 §2 atomic idempotency. Verifies the pipeline behavior: reserved → run once + store result; completed →
/// replay stored result without running; in-flight duplicate → 409; handler failure → release for retry;
/// non-idempotent commands pass straight through.
/// </summary>
public class IdempotencyBehaviorTests
{
    public sealed record IdemCommand(Guid? IdempotencyKey) : ICommand<string>, IRequireIdempotency;
    public sealed record PlainCommand(Guid? IdempotencyKey) : ICommand<string>;

    private readonly IIdempotencyStore _store = Substitute.For<IIdempotencyStore>();
    private readonly IServiceProvider _sp = Substitute.For<IServiceProvider>();

    public IdempotencyBehaviorTests() => _sp.GetService(typeof(IIdempotencyStore)).Returns(_store);

    private IdempotencyBehavior<TReq, string> Behavior<TReq>() where TReq : class, ICommand<string>
        => new(_sp, NullLogger<IdempotencyBehavior<TReq, string>>.Instance);

    [Fact]
    public async Task Non_idempotent_command_passes_through_without_touching_the_store()
    {
        var calls = 0;
        RequestHandlerDelegate<string> next = _ => { calls++; return Task.FromResult("ok"); };

        var result = await Behavior<PlainCommand>().Handle(new PlainCommand(Guid.NewGuid()), next, CancellationToken.None);

        result.Should().Be("ok");
        calls.Should().Be(1);
        await _store.DidNotReceive().TryReserveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reserved_runs_the_handler_once_and_stores_the_result()
    {
        _store.TryReserveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyOutcome(IdempotencyState.Reserved, null));
        var calls = 0;
        RequestHandlerDelegate<string> next = _ => { calls++; return Task.FromResult("charged"); };

        var result = await Behavior<IdemCommand>().Handle(new IdemCommand(Guid.NewGuid()), next, CancellationToken.None);

        result.Should().Be("charged");
        calls.Should().Be(1);
        await _store.Received(1).CompleteAsync(Arg.Any<string>(), Arg.Any<string>(),
            Arg.Is<string>(j => j.Contains("charged")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Completed_replays_the_stored_result_without_running_the_handler()
    {
        _store.TryReserveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyOutcome(IdempotencyState.Completed, "\"previously-charged\""));
        var calls = 0;
        RequestHandlerDelegate<string> next = _ => { calls++; return Task.FromResult("charged-again"); };

        var result = await Behavior<IdemCommand>().Handle(new IdemCommand(Guid.NewGuid()), next, CancellationToken.None);

        result.Should().Be("previously-charged");
        calls.Should().Be(0, "a completed request must never re-run the handler (no double charge)");
    }

    [Fact]
    public async Task InFlight_duplicate_throws_conflict_and_never_runs_the_handler()
    {
        _store.TryReserveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyOutcome(IdempotencyState.InFlight, null));
        var calls = 0;
        RequestHandlerDelegate<string> next = _ => { calls++; return Task.FromResult("x"); };

        var act = async () => await Behavior<IdemCommand>().Handle(new IdemCommand(Guid.NewGuid()), next, CancellationToken.None);

        await act.Should().ThrowAsync<IdempotencyConflictException>();
        calls.Should().Be(0);
        await _store.DidNotReceive().CompleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handler_failure_releases_the_reservation_so_the_request_can_be_retried()
    {
        _store.TryReserveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyOutcome(IdempotencyState.Reserved, null));
        RequestHandlerDelegate<string> next = _ => throw new InvalidOperationException("gateway down");

        var act = async () => await Behavior<IdemCommand>().Handle(new IdemCommand(Guid.NewGuid()), next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _store.Received(1).ReleaseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _store.DidNotReceive().CompleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
