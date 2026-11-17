using System.Text.Json;
using ControlM.Client.Operations;
using Shouldly;

namespace ControlM.Conformance.Tests.Topology.GetServers;

public sealed class GetServersTests
{
    [Test]
    public async Task A_client_reads_the_servers_as_an_array()
    {
        await using var target = await ConformanceTarget.StartAsync();

        var servers = await target.Client.SendAsync<JsonElement>(
            Operation.GetServers.Method, Operation.GetServers.Path);

        servers.ValueKind.ShouldBe(JsonValueKind.Array);
    }
}
