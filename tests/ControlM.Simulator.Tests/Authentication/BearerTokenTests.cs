using System.Net;
using System.Net.Http.Headers;
using System.Text;
using ControlM.Fixtures;
using ControlM.Simulator.Hosting;
using ControlM.Simulator.Tests.Time;
using Shouldly;

namespace ControlM.Simulator.Tests.Authentication;

public sealed class BearerTokenTests
{
    private static readonly TimeSpan s_lifetime = TimeSpan.FromMinutes(10);

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

    [Test]
    public async Task A_token_is_refused_once_the_session_lifetime_has_passed_unused()
    {
        var time = new FakeTimeProvider();
        await using var simulator = await StartWithUserAsync(time);
        using var http = simulator.CreateHttpClient();
        await LogInAsync(http);

        time.Advance(s_lifetime);
        using var response = await GetServersWithTokenAsync(http, "SIMTOKEN1");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(
            JsonFixture.Load("Authentication", "ApiKey", "GetServers.invalid-bearer-token.json"));
    }

    private static async Task<ControlMSimulator> StartWithUserAsync(TimeProvider time)
    {
        var simulator = await ControlMSimulator.StartAsync(o =>
        {
            o.TimeProvider = time;
            o.SessionLifetime = s_lifetime;
        });
        simulator.Credentials.AddUser("USER1", "PASSWORD1");
        return simulator;
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
