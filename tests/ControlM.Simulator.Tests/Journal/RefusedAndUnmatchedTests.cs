using ControlM.Client.Operations;
using ControlM.Simulator.Hosting;
using Shouldly;

namespace ControlM.Simulator.Tests.Journal;

public sealed class RefusedAndUnmatchedTests
{
    [Test]
    public async Task A_request_refused_for_its_credential_keeps_its_operation_and_counts()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        using var http = simulator.CreateHttpClient();

        using var response = await http.GetAsync(new Uri("/automation-api/config/servers", UriKind.Relative));

        var entry = simulator.Journal.Entries.ShouldHaveSingleItem();
        entry.Operation.ShouldBe(Operation.GetServers);
        entry.Status.ShouldBe(401);
        simulator.Journal.Count(Operation.GetServers).ShouldBe(1);
    }
}
