using ControlM.Client.Operations;
using ControlM.Simulator.Hosting;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace ControlM.Simulator.Tests.Journal;

public sealed class RequestJournalTests
{
    private static readonly DateTimeOffset _arrival = new(2026, 3, 14, 9, 26, 53, TimeSpan.Zero);

    private static Uri GetServers => new("/automation-api/config/servers", UriKind.Relative);

    [Test]
    public async Task An_accepted_request_is_one_entry_with_its_operation_method_path_query_and_status()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        simulator.Credentials.AddApiKey("APIKEY1");
        using var http = simulator.CreateHttpClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri("/automation-api/config/servers?server=SERVER1", UriKind.Relative));
        request.Headers.Add("x-api-key", "APIKEY1");

        using var response = await http.SendAsync(request);

        var entry = simulator.Journal.Entries.ShouldHaveSingleItem();
        entry.Operation.ShouldBe(Operation.GetServers);
        entry.Method.ShouldBe("GET");
        entry.Path.ShouldBe("/automation-api/config/servers");
        entry.Query.ShouldBe("server=SERVER1");
        entry.Status.ShouldBe(200);
    }

    [Test]
    public async Task An_entry_has_the_time_the_request_arrived()
    {
        var time = new FakeTimeProvider(_arrival);
        await using var simulator = await ControlMSimulator.StartAsync(o => o.TimeProvider = time);
        using var http = simulator.CreateHttpClient();

        using var response = await http.GetAsync(GetServers);

        simulator.Journal.Entries.ShouldHaveSingleItem().Time.ShouldBe(_arrival);
    }

    [Test]
    public async Task Entries_are_in_arrival_order_when_requests_share_a_time()
    {
        var time = new FakeTimeProvider(_arrival);
        await using var simulator = await ControlMSimulator.StartAsync(o => o.TimeProvider = time);
        using var http = simulator.CreateHttpClient();

        using var first = await http.GetAsync(new Uri("/automation-api/config/servers?server=SERVER1", UriKind.Relative));
        using var second = await http.GetAsync(new Uri("/automation-api/config/servers?server=SERVER2", UriKind.Relative));

        simulator.Journal.Entries.Select(entry => entry.Query).ShouldBe(["server=SERVER1", "server=SERVER2"]);
    }

    [Test]
    public async Task A_snapshot_does_not_change_when_a_later_request_arrives()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        using var http = simulator.CreateHttpClient();
        using var first = await http.GetAsync(GetServers);
        var snapshot = simulator.Journal.Entries;

        using var second = await http.GetAsync(GetServers);

        snapshot.Count.ShouldBe(1);
    }
}
