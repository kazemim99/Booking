using System.Reflection;
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Infrastructure.External.Payment;
using FluentAssertions;

namespace Booksy.ArchitectureTests;

/// <summary>
/// Enforces ADR-006's central invariant: <b>a gateway call must never happen inside a retried
/// transaction.</b>
///
/// <para><b>The failure it prevents.</b> <c>TransactionBehavior</c> runs every command inside
/// <c>IUnitOfWork.ExecuteInTransactionAsync</c>, which executes on Npgsql's <b>retrying</b> execution
/// strategy (<c>EnableRetryOnFailure</c>). A handler that calls a payment gateway from inside that scope
/// will, on a transient DB fault, re-run in its entirety — <b>including the gateway call</b>. The
/// customer is charged or refunded twice. Reconciliation cannot save this: it prevents a duplicate
/// <i>record</i>, never a duplicate <i>charge</i>.</para>
///
/// <para><b>The rule.</b> If a command handler takes <c>IPaymentGateway</c> as a dependency, its command
/// must implement <see cref="INonTransactionalCommand"/>, which opts it out of the ambient retried
/// transaction so the gateway call happens exactly once.</para>
///
/// <para><b>Why reflection rather than NetArchTest.</b> The rule is a relationship between two types —
/// a handler's constructor dependency and its command's marker interface. NetArchTest expresses
/// "type A must not depend on B", which cannot cross that link. Reflection over the closed generic
/// handler interface can, and reports the offending command by name.</para>
/// </summary>
public class MoneyCommandTransactionTests
{
    private static readonly Assembly ApplicationAssembly = typeof(Booksy.ServiceCatalog.Application.Commands.Payment.RefundPayment.RefundPaymentCommand).Assembly;

    /// <summary>
    /// Maps a handler type to the command it handles, via its closed <c>IRequestHandler&lt;,&gt;</c> or
    /// <c>IRequestHandler&lt;&gt;</c> interface.
    /// </summary>
    private static Type? CommandHandledBy(Type handlerType) =>
        handlerType.GetInterfaces()
            .Where(i => i.IsGenericType)
            .Where(i => i.GetGenericTypeDefinition().Name.StartsWith("IRequestHandler", StringComparison.Ordinal)
                     || i.GetGenericTypeDefinition().Name.StartsWith("ICommandHandler", StringComparison.Ordinal))
            .Select(i => i.GetGenericArguments().FirstOrDefault())
            .FirstOrDefault(t => t is not null);

    /// <summary>
    /// The ways a handler can reach an external payment system. It is deliberately not just
    /// <c>IPaymentGateway</c>: the ZarinPal handlers take <c>IZarinPalService</c> and the capture
    /// handler resolves through <c>IPaymentGatewayFactory</c>, and all three move real money. Matching
    /// only the one interface would have let two live commands past the rule below.
    /// </summary>
    private static readonly string[] GatewayDependencyNames =
    {
        "IPaymentGateway",
        "IPaymentGatewayFactory",
        "IZarinPalService",
    };

    private static bool DependsOnPaymentGateway(Type handlerType) =>
        handlerType.GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Any(p => typeof(IPaymentGateway).IsAssignableFrom(p.ParameterType)
                   || GatewayDependencyNames.Contains(p.ParameterType.Name));

    [Fact]
    public void Handlers_That_Call_A_Payment_Gateway_Must_Handle_A_NonTransactional_Command()
    {
        var offenders = new List<string>();

        foreach (var handler in ApplicationAssembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false }))
        {
            if (!DependsOnPaymentGateway(handler))
            {
                continue;
            }

            var command = CommandHandledBy(handler);
            if (command is null)
            {
                // Depends on the gateway but is not a command handler (e.g. a reconciler or background
                // service). Those run outside TransactionBehavior entirely, so the rule does not apply.
                continue;
            }

            if (!typeof(INonTransactionalCommand).IsAssignableFrom(command))
            {
                offenders.Add($"{handler.Name} takes IPaymentGateway but {command.Name} does not implement INonTransactionalCommand");
            }
        }

        offenders.Should().BeEmpty(
            "ADR-006: a handler that calls a payment gateway inside the ambient retried transaction will " +
            "re-issue the gateway call when a transient DB fault retries the command — double-charging or " +
            "double-refunding. Mark the command with INonTransactionalCommand and make the handler commit " +
            "its own single retry-safe unit. Offenders:" + Environment.NewLine + "  " +
            string.Join(Environment.NewLine + "  ", offenders));
    }

    /// <summary>
    /// The converse guard: <see cref="INonTransactionalCommand"/> opts a command out of the ambient
    /// transaction, so it is not a free-to-apply annotation. Anything carrying it must actually be a
    /// money-moving command, or it has silently lost its transactional guarantees.
    /// </summary>
    [Fact]
    public void Only_Money_Moving_Commands_Opt_Out_Of_The_Ambient_Transaction()
    {
        var handlersByCommand = ApplicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Select(t => (Handler: t, Command: CommandHandledBy(t)))
            .Where(x => x.Command is not null)
            .ToLookup(x => x.Command!, x => x.Handler);

        var optedOut = ApplicationAssembly.GetTypes()
            .Where(t => typeof(INonTransactionalCommand).IsAssignableFrom(t))
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .ToList();

        optedOut.Should().NotBeEmpty(
            "the marker exists precisely because money commands need it; if nothing carries it, either " +
            "the marker was removed or the money commands silently lost their opt-out");

        var suspicious = optedOut
            .Where(command => handlersByCommand[command].Any() &&
                              !handlersByCommand[command].Any(DependsOnPaymentGateway))
            .Select(command => command.Name)
            .OrderBy(x => x)
            .ToList();

        // CapturePaymentCommand is a known, deliberate exception rather than an approved pattern.
        // Its handler takes no gateway dependency at all — it loads the payment, calls payment.Capture(),
        // updates and commits. Its marker comment claims "gateway capture must not run in a retried tx",
        // but no gateway capture happens. The endpoint (POST /Payments/{id}/capture) is live, and looks
        // vestigial from a Stripe-shaped authorize/capture flow: ZarinPal, the only enabled gateway, is
        // redirect-and-verify and has no separate capture step. The marker is left in place because
        // removing it would change the transaction semantics of a live money endpoint, which needs a
        // decision about what capture should mean here, not a drive-by edit.
        // Recorded in openspec/changes/FOLLOW-UPS.md.
        suspicious.Should().BeSubsetOf(
            new[] { "CapturePaymentCommand" },
            "a command that opts out of the ambient transaction but never reaches an external payment " +
            "system has given up its transactional guarantee for nothing — confirm it really moves money, " +
            "or drop the marker. If it does move money through a new abstraction, add that abstraction to " +
            $"{nameof(GatewayDependencyNames)} so the forward rule above can see it too");
    }
}
