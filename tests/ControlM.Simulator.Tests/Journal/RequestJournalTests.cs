using System.Net;
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
    public async Task A_request_answered_after_a_later_one_still_comes_first()
    {
        var clock = new ClockThatSignalsItsFirstReading(_arrival);
        await using var simulator = await ControlMSimulator.StartAsync(o => o.TimeProvider = clock);
        using var http = simulator.CreateHttpClient();
        var heldBody = new BodyHeldUntilReleased();

        var first = http.PostAsync(new Uri("/automation-api/no/such/path?request=FIRST", UriKind.Relative), heldBody);
        await clock.FirstReading;
        using var second = await http.GetAsync(new Uri("/automation-api/no/such/path?request=SECOND", UriKind.Relative));
        heldBody.Release();
        using var firstResponse = await first;

        simulator.Journal.Entries.Select(entry => entry.Query).ShouldBe(["request=FIRST", "request=SECOND"]);
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

    [Test]
    public async Task Two_instances_keep_separate_journals()
    {
        await using var first = await ControlMSimulator.StartAsync();
        await using var second = await ControlMSimulator.StartAsync();
        using var http = first.CreateHttpClient();

        using var response = await http.GetAsync(GetServers);

        first.Journal.Entries.Count.ShouldBe(1);
        second.Journal.Entries.ShouldBeEmpty();
    }

    [Test]
    public async Task Concurrent_requests_are_all_recorded()
    {
        const int requests = 200;
        await using var simulator = await ControlMSimulator.StartAsync();
        using var http = simulator.CreateHttpClient();

        var responses = await Task.WhenAll(Enumerable.Range(0, requests).Select(_ => Task.Run(() => http.GetAsync(GetServers))));

        foreach (var response in responses)
        {
            response.Dispose();
        }

        simulator.Journal.Entries.Count.ShouldBe(requests);
    }

    [Test]
    public async Task The_session_operations_are_recorded_with_their_operations()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        using var http = simulator.CreateHttpClient();

        using var login = await http.PostAsync(new Uri("/automation-api/session/login", UriKind.Relative), null);
        using var logout = await http.PostAsync(new Uri("/automation-api/session/logout", UriKind.Relative), null);

        simulator.Journal.Entries.Select(entry => entry.Operation).ShouldBe([Operation.Login, Operation.Logout]);
    }

    // The journal reads the clock when a request arrives, so the first reading shows the first request has arrived.
    private sealed class ClockThatSignalsItsFirstReading(DateTimeOffset now) : TimeProvider
    {
        private readonly TaskCompletionSource _firstReading = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task FirstReading => _firstReading.Task;

        public override DateTimeOffset GetUtcNow()
        {
            _firstReading.TrySetResult();
            return now;
        }
    }

    // A body the client starts sending only when the test releases it, so the request stays unanswered until then.
    private sealed class BodyHeldUntilReleased : HttpContent
    {
        private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Release() => _released.TrySetResult();

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            await _released.Task;
            await stream.WriteAsync("FIRST"u8.ToArray());
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
