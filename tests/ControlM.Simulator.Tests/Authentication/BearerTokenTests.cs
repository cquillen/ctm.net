using System.Net;
using System.Net.Http.Headers;
using System.Text;
using ControlM.Fixtures;
using ControlM.Simulator.Hosting;
using Shouldly;

namespace ControlM.Simulator.Tests.Authentication;

public sealed class BearerTokenTests
{
    private static Uri GetServers => new("/automation-api/config/servers", UriKind.Relative);

    [Test]
    public async Task A_token_from_login_is_accepted()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        simulator.Credentials.AddUser("USER1", "PASSWORD1");
        using var http = simulator.CreateHttpClient();
        await LogInAsync(http);

        using var response = await GetServersWithTokenAsync(http, "SIMTOKEN1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(
            JsonFixture.Load("Topology", "GetServers", "GetServers.empty.json"));
    }

    [Test]
    public async Task A_token_no_login_issued_answers_401_with_the_observed_body()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        simulator.Credentials.AddUser("USER1", "PASSWORD1");
        using var http = simulator.CreateHttpClient();
        await LogInAsync(http);

        using var response = await GetServersWithTokenAsync(http, "SIMTOKEN2");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(
            JsonFixture.Load("Authentication", "ApiKey", "GetServers.invalid-bearer-token.json"));
    }

    private static async Task LogInAsync(HttpClient http)
    {
        using var response = await http.PostAsync(
            new Uri("/automation-api/session/login", UriKind.Relative),
            new StringContent("""{"username":"USER1","password":"PASSWORD1"}""", Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
    }

    private static async Task<HttpResponseMessage> GetServersWithTokenAsync(HttpClient http, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, GetServers);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await http.SendAsync(request);
    }
}
