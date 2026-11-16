using ControlM.Simulator.Hosting;
using Shouldly;

namespace ControlM.Simulator.Tests.Journal;

public sealed class RequestContentTests
{
    private static Uri GetServers => new("/automation-api/config/servers", UriKind.Relative);

    [Test]
    public async Task The_credential_headers_are_kept_as_sent_and_read_back_with_any_casing()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        using var http = simulator.CreateHttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, GetServers);
        request.Headers.Add("x-api-key", "APIKEY1");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer TOKEN1");

        using var response = await http.SendAsync(request);

        var headers = simulator.Journal.Entries.ShouldHaveSingleItem().Headers;
        headers["X-API-KEY"].ShouldBe("APIKEY1");
        headers["authorization"].ShouldBe("Bearer TOKEN1");
    }

    [Test]
    public async Task A_repeated_header_is_one_string_with_its_values_joined()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        using var http = simulator.CreateHttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, GetServers);
        request.Headers.Add("X-Correlation", ["CORRELATION1", "CORRELATION2"]);

        using var response = await http.SendAsync(request);

        simulator.Journal.Entries.ShouldHaveSingleItem().Headers["X-Correlation"].ShouldBe("CORRELATION1, CORRELATION2");
    }
}
