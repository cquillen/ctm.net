using System.Text.Json;
using ControlM.Client;
using ControlM.Client.Operations;
using ControlM.Simulator.Hosting;
using Shouldly;

namespace ControlM.Conformance.Tests.Journal;

public sealed class JournalTests
{
    [Test]
    [Category("SimulatorOnly")]
    public async Task A_call_through_the_client_is_counted_for_its_operation()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        IControlMClient client = simulator.CreateClient();

        await client.SendAsync<JsonElement>(Operation.GetServers.Method, Operation.GetServers.Path);

        simulator.Journal.Count(Operation.GetServers).ShouldBe(1);
    }

    [Test]
    [Category("SimulatorOnly")]
    public async Task Valid_calls_through_the_client_leave_nothing_unmatched()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        IControlMClient client = simulator.CreateClient();

        await client.SendAsync<JsonElement>(Operation.GetServers.Method, Operation.GetServers.Path);
        await client.SendAsync<JsonElement>(Operation.GetServers.Method, Operation.GetServers.Path);

        simulator.Journal.Entries.Count.ShouldBe(2);
        simulator.Journal.Unmatched.ShouldBeEmpty();
    }
}
