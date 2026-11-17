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

        using var response = await http.GetAsync(RequestUris.GetServers);

        var entry = simulator.Journal.Entries.ShouldHaveSingleItem();
        entry.Operation.ShouldBe(Operation.GetServers);
        entry.Status.ShouldBe(401);
        simulator.Journal.Count(Operation.GetServers).ShouldBe(1);
    }

    [Test]
    public async Task An_unknown_path_under_the_api_is_unmatched()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        using var http = simulator.CreateHttpClient();

        using var response = await http.GetAsync(new Uri("/automation-api/config/nowhere", UriKind.Relative));

        var entry = simulator.Journal.Unmatched.ShouldHaveSingleItem();
        entry.Operation.ShouldBeNull();
        entry.Path.ShouldBe("/automation-api/config/nowhere");
        entry.Status.ShouldBe(404);
    }

    [Test]
    public async Task A_path_outside_the_api_is_unmatched_with_its_whole_path()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        using var http = simulator.CreateHttpClient();

        using var response = await http.GetAsync(new Uri("/config/servers", UriKind.Relative));

        var entry = simulator.Journal.Unmatched.ShouldHaveSingleItem();
        entry.Operation.ShouldBeNull();
        entry.Path.ShouldBe("/config/servers");
        entry.Status.ShouldBe(404);
    }

    [Test]
    public async Task A_wrong_method_on_a_real_path_is_unmatched()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        using var http = simulator.CreateHttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Patch, RequestUris.GetServers);

        using var response = await http.SendAsync(request);

        var entry = simulator.Journal.Unmatched.ShouldHaveSingleItem();
        entry.Operation.ShouldBeNull();
        entry.Method.ShouldBe("PATCH");
        entry.Status.ShouldBe(405);
    }

    [Test]
    public async Task A_request_to_the_control_api_is_not_journaled()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        using var http = simulator.CreateHttpClient();

        using var response = await http.GetAsync(new Uri("/__sim/journal", UriKind.Relative));

        simulator.Journal.Entries.ShouldBeEmpty();
    }
}
