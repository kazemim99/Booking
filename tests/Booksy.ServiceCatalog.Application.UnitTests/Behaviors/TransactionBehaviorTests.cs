using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Behaviors;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Booksy.ServiceCatalog.Application.UnitTests.Behaviors;

/// <summary>
/// C2 §1 (correctness under failure). A money-moving command marked <see cref="INonTransactionalCommand"/> must
/// bypass the ambient <c>ExecuteInTransactionAsync</c> (which runs on a retrying Npgsql execution strategy), so a
/// transient-fault retry can never re-execute the handler's external gateway call and double-charge / double-refund.
/// </summary>
public class TransactionBehaviorTests
{
    public sealed record NormalCommand : IRequest<string>;
    public sealed record MoneyCommand : IRequest<string>, INonTransactionalCommand;

    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private TransactionBehavior<TRequest, string> Behavior<TRequest>() where TRequest : class, IRequest<string>
        => new(NullLogger<TransactionBehavior<TRequest, string>>.Instance, _uow);

    [Fact]
    public async Task NonTransactional_command_bypasses_the_ambient_retrying_transaction()
    {
        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        var result = await Behavior<MoneyCommand>().Handle(new MoneyCommand(), next, CancellationToken.None);

        result.Should().Be("ok");
        await _uow.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<Task<string>>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Normal_command_runs_inside_the_ambient_transaction()
    {
        _uow.HasActiveTransaction.Returns(false);
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<Task<string>>>()());

        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        var result = await Behavior<NormalCommand>().Handle(new NormalCommand(), next, CancellationToken.None);

        result.Should().Be("ok");
        await _uow.Received(1).ExecuteInTransactionAsync(
            Arg.Any<Func<Task<string>>>(), Arg.Any<CancellationToken>());
    }
}
