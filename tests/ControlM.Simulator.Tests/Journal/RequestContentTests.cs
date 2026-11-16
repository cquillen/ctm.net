using System.Net;
using System.Text;
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

    [Test]
    public async Task A_login_body_is_kept_verbatim_and_the_login_still_reads_it()
    {
        const string body = """{ "username": "USER1", "password": "PASSWORD1" }""";
        await using var simulator = await ControlMSimulator.StartAsync();
        simulator.Credentials.AddUser("USER1", "PASSWORD1");
        using var http = simulator.CreateHttpClient();

        using var response = await http.PostAsync(
            new Uri("/automation-api/session/login", UriKind.Relative),
            new StringContent(body, Encoding.UTF8, "application/json"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        simulator.Journal.Entries.ShouldHaveSingleItem().Body.ShouldBe(body);
    }

    [Test]
    public async Task A_body_sent_to_an_unknown_path_is_kept_verbatim()
    {
        const string body = "FOLDER1 is not a real request";
        await using var simulator = await ControlMSimulator.StartAsync();
        using var http = simulator.CreateHttpClient();

        using var response = await http.PostAsync(
            new Uri("/automation-api/no/such/path", UriKind.Relative),
            new StringContent(body, Encoding.UTF8, "text/plain"));

        simulator.Journal.Entries.ShouldHaveSingleItem().Body.ShouldBe(body);
    }

    [Test]
    public async Task A_request_without_a_body_has_an_empty_body()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        using var http = simulator.CreateHttpClient();

        using var response = await http.GetAsync(GetServers);

        simulator.Journal.Entries.ShouldHaveSingleItem().Body.ShouldBeEmpty();
    }
}
