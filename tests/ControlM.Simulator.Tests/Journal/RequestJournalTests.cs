using ControlM.Client.Operations;
using ControlM.Simulator.Hosting;
using Shouldly;

namespace ControlM.Simulator.Tests.Journal;

public sealed class RequestJournalTests
{
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
}
