using ControlM.Simulator.Hosting;
using Shouldly;

namespace ControlM.Simulator.Tests.Hosting;

public sealed class ControlMSimulatorTests
{
    [Test]
    public async Task The_options_callback_sees_the_system_time_provider_by_default()
    {
        TimeProvider? seen = null;

        await using var simulator = await ControlMSimulator.StartAsync(o => seen = o.TimeProvider);

        seen.ShouldBeSameAs(TimeProvider.System);
    }
}
