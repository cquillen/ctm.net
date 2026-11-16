using System.Net;
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

    [Test]
    public async Task An_instance_keeps_answering_after_another_started_beside_it_is_disposed()
    {
        await using var survivor = await ControlMSimulator.StartAsync();
        var disposed = await ControlMSimulator.StartAsync();
        await disposed.DisposeAsync();
        using var http = survivor.CreateHttpClient();

        using var response = await http.GetAsync(new Uri("/automation-api/no/such/path", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
