namespace AsanRezerve.Host;

/// <summary>
/// Marker type used by integration tests to locate the <c>AsanRezerve.Host</c> assembly, so
/// <c>WebApplicationFactory&lt;T&gt;</c> boots the real composed host — the same top-level
/// <c>Program</c> the container runs — rather than a bounded context's own retired entry point.
///
/// <para><b>Why a marker instead of <c>Program</c>.</b> <c>AsanRezerve.Host</c> and
/// <c>AsanRezerve.UserManagement.API</c> both declare a global <c>Program</c>, so naming it directly is
/// ambiguous (CS0433). <c>WebApplicationFactory</c> only uses its type argument to find the assembly,
/// so any public type from this assembly serves. <c>HostCompositionFactory</c> solves the same problem
/// by pointing at <c>InProcessProviderInfoService</c>; this type exists so callers do not have to pick
/// an unrelated service class and can see at a glance what the type argument is for.</para>
/// </summary>
public sealed class HostEntryPoint
{
}
