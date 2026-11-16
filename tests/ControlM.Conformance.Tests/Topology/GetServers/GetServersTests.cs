using System.Text.Json;
using ControlM.Client;
using ControlM.Client.Operations;
using ControlM.Simulator.Hosting;
using Shouldly;

namespace ControlM.Conformance.Tests.Topology.GetServers;

public sealed class GetServersTests
{
    [Test]
    [Category("SimulatorOnly")]
    public async Task A_client_with_an_api_key_reads_the_servers_as_an_array()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        IControlMClient client = simulator.CreateClient();

        var servers = await client.SendAsync<JsonElement>(Operation.GetServers.Method, Operation.GetServers.Path);

        servers.ValueKind.ShouldBe(JsonValueKind.Array);
    }
}
