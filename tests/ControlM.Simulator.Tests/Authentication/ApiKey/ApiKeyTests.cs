using System.Net;
using System.Net.Http.Headers;
using ControlM.Fixtures;
using ControlM.Simulator.Hosting;
using Shouldly;

namespace ControlM.Simulator.Tests.Authentication.ApiKey;

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

    // The body for an unknown key is assumed (requirements section 4.8): no API token was available to probe.
    [Test]
    public async Task A_key_that_was_never_seeded_answers_401_with_the_assumed_body()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        simulator.Credentials.AddApiKey("APIKEY1");
        using var http = simulator.CreateHttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, GetServers);
        request.Headers.Add("x-api-key", "APIKEY2");

        using var response = await http.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(
            JsonFixture.Load("Authentication", "ApiKey", "GetServers.unknown-api-key.json"));
    }
}
