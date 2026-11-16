using System.Net;
using System.Net.Http.Headers;
using ControlM.Fixtures;
using ControlM.Simulator.Hosting;
using Shouldly;

namespace ControlM.Simulator.Tests.Authentication;

public sealed class ApiKeyTests
{
    private static Uri GetServers => new("/automation-api/config/servers", UriKind.Relative);

    [Test]
    public async Task A_request_with_no_credential_answers_401_with_the_observed_body()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        using var http = simulator.CreateHttpClient();

        using var response = await http.GetAsync(GetServers);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(
            JsonFixture.Load("Authentication", "ApiKey", "GetServers.no-credential.json"));
    }

    [Test]
    public async Task A_bearer_token_alone_answers_401_with_the_observed_body()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        using var http = simulator.CreateHttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, GetServers);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "TOKEN1");

        using var response = await http.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(
            JsonFixture.Load("Authentication", "ApiKey", "GetServers.invalid-bearer-token.json"));
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task A_key_beside_an_authorization_header_answers_401_with_the_observed_body(bool seeded)
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        if (seeded)
        {
            simulator.Credentials.AddApiKey("APIKEY1");
        }

        using var http = simulator.CreateHttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, GetServers);
        request.Headers.Add("x-api-key", "APIKEY1");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "TOKEN1");

        using var response = await http.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(
            JsonFixture.Load("Authentication", "ApiKey", "GetServers.both-headers.json"));
    }

    [Test]
    public async Task A_key_seeded_on_one_instance_is_unknown_to_another()
    {
        await using var seeded = await ControlMSimulator.StartAsync();
        await using var other = await ControlMSimulator.StartAsync();
        seeded.Credentials.AddApiKey("APIKEY1");
        using var http = other.CreateHttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, GetServers);
        request.Headers.Add("x-api-key", "APIKEY1");

        using var response = await http.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(
            JsonFixture.Load("Authentication", "ApiKey", "GetServers.unknown-api-key.json"));
    }

    [Test]
    public async Task A_key_that_was_never_seeded_answers_400_with_the_observed_body()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        simulator.Credentials.AddApiKey("APIKEY1");
        using var http = simulator.CreateHttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, GetServers);
        request.Headers.Add("x-api-key", "APIKEY2");

        using var response = await http.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(
            JsonFixture.Load("Authentication", "ApiKey", "GetServers.unknown-api-key.json"));
    }
}
