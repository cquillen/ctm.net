using System.Net;
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
}
