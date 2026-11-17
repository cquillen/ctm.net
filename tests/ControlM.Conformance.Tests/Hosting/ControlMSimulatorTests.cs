using System.Net;
using ControlM.Client.Errors;
using ControlM.Simulator.Hosting;
using Shouldly;

namespace ControlM.Conformance.Tests.Hosting;

public sealed class ControlMSimulatorTests
{
    [Test]
    [Category("SimulatorOnly")]
    public async Task A_client_keeps_reaching_its_own_instance_after_another_is_disposed()
    {
        await using var survivor = await ControlMSimulator.StartAsync();
        var disposed = await ControlMSimulator.StartAsync();
        disposed.CreateClient();
        var survivorClient = survivor.CreateClient();
        await disposed.DisposeAsync();

        var exception = await Should.ThrowAsync<ControlMNotFoundException>(
            () => survivorClient.SendAsync(HttpMethod.Get, "/no/such/path"));

        exception.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    [Category("SimulatorOnly")]
    public async Task The_client_callback_runs_after_the_endpoint_is_set_so_it_can_replace_it()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        var client = simulator.CreateClient(o =>
        {
            o.Endpoint = new Uri("http://localhost/elsewhere");
            o.UseApiKey("APIKEY1");
        });

        var exception = await Should.ThrowAsync<ControlMNotFoundException>(
            () => client.SendAsync(HttpMethod.Get, "/config/servers"));

        exception.Errors.ShouldHaveSingleItem().Uri.ShouldBe("/elsewhere/config/servers");
    }
}
