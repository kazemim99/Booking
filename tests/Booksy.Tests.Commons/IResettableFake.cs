namespace Booksy.Tests.Commons;

/// <summary>
/// A test double that records what it was asked to do (a captured SMS body, an e-mail subject) and
/// needs clearing between tests now that a factory — and therefore the fake — is shared across a
/// whole test class rather than rebuilt per class. Implemented by fakes that hold state; stateless
/// fakes (payment gateway, push, in-app) need nothing here.
///
/// <para>Register a fake a second time as this interface, pointing at the same singleton instance,
/// so <c>TestWebApplicationFactory.ResetStateAsync</c> can find and reset it without knowing the
/// fake's own service interface:
/// <code>services.AddSingleton&lt;IResettableFake&gt;(sp =&gt; (IResettableFake)sp.GetRequiredService&lt;IEmailNotificationService&gt;());</code>
/// </para>
/// </summary>
public interface IResettableFake
{
    void Reset();
}
