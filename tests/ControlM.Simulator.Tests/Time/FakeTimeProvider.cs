namespace ControlM.Simulator.Tests.Time;

// Written here rather than taken from Microsoft.Extensions.TimeProvider.Testing: these tests only read the time and
// move it forward, and the change that added it was to add no package.
internal sealed class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan delta) => _now += delta;
}
