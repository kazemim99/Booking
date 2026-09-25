namespace AsanRezerve.Infrastructure.Observability.UnitTests;

/// <summary>A clock a test moves by hand.</summary>
internal sealed class ManualTimeProvider : TimeProvider
{
    private DateTimeOffset _now = new(2026, 9, 25, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}
