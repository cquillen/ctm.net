using System.Net;
using System.Net.Http.Headers;
using System.Text;
using ControlM.Fixtures;
using ControlM.Simulator.Hosting;
using Shouldly;

namespace ControlM.Simulator.Tests.Authentication.Logout;

public sealed class LogoutTests
{
    private static Uri Logout => new("/automation-api/session/logout", UriKind.Relative);

    private static Uri GetServers => new("/automation-api/config/servers", UriKind.Relative);

    [Test]
    public async Task A_live_token_answers_200_with_the_observed_body_and_its_session_ends()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        simulator.Credentials.AddUser("USER1", "PASSWORD1");
        using var http = simulator.CreateHttpClient();
        await LogInAsync(http);

        using var response = await SendWithTokenAsync(http, HttpMethod.Post, Logout, "SIMTOKEN1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(
            JsonFixture.Load("Authentication", "Logout", "Logout.success.json"));
        using var afterLogout = await SendWithTokenAsync(http, HttpMethod.Get, GetServers, "SIMTOKEN1");
        afterLogout.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await afterLogout.Content.ReadAsStringAsync()).ShouldEqualJson(
            JsonFixture.Load("Authentication", "ApiKey", "GetServers.invalid-bearer-token.json"));
    }

    [Test]
    public async Task A_token_that_has_already_logged_out_answers_the_same_200()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        simulator.Credentials.AddUser("USER1", "PASSWORD1");
        using var http = simulator.CreateHttpClient();
        await LogInAsync(http);
        using var firstLogout = await SendWithTokenAsync(http, HttpMethod.Post, Logout, "SIMTOKEN1");

        using var response = await SendWithTokenAsync(http, HttpMethod.Post, Logout, "SIMTOKEN1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(
            JsonFixture.Load("Authentication", "Logout", "Logout.success.json"));
    }

    [Test]
    public async Task A_token_no_login_issued_answers_200_naming_that_token()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        using var http = simulator.CreateHttpClient();

        using var response = await SendWithTokenAsync(http, HttpMethod.Post, Logout, "SIMTOKEN7");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(
            """{"message":"Successfully logged out from session SIMTOKEN7"}""");
    }

    [Test]
    public async Task No_credential_header_answers_the_no_credential_401()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        using var http = simulator.CreateHttpClient();

        using var response = await http.PostAsync(Logout, content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(
            JsonFixture.Load("Authentication", "ApiKey", "GetServers.no-credential.json"));
    }

    [Test]
    public async Task Both_credential_headers_answer_the_both_headers_401()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        simulator.Credentials.AddUser("USER1", "PASSWORD1");
        simulator.Credentials.AddApiKey("APIKEY1");
        using var http = simulator.CreateHttpClient();
        await LogInAsync(http);
        using var request = new HttpRequestMessage(HttpMethod.Post, Logout);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "SIMTOKEN1");
        request.Headers.Add("x-api-key", "APIKEY1");

        using var response = await http.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(
            JsonFixture.Load("Authentication", "ApiKey", "GetServers.both-headers.json"));
        using var afterRefusal = await SendWithTokenAsync(http, HttpMethod.Get, GetServers, "SIMTOKEN1");
        afterRefusal.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // Assumed (requirements section 4.8): no real server has been sent a logout with an API key.
    [Test]
    public async Task An_api_key_alone_answers_200_naming_the_key_and_ends_no_session()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        simulator.Credentials.AddUser("USER1", "PASSWORD1");
        using var http = simulator.CreateHttpClient();
        await LogInAsync(http);
        using var request = new HttpRequestMessage(HttpMethod.Post, Logout);
        request.Headers.Add("x-api-key", "APIKEY1");

        using var response = await http.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(
            """{"message":"Successfully logged out from session APIKEY1"}""");
        using var afterLogout = await SendWithTokenAsync(http, HttpMethod.Get, GetServers, "SIMTOKEN1");
        afterLogout.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task LogInAsync(HttpClient http)
    {
        using var response = await http.PostAsync(
            new Uri("/automation-api/session/login", UriKind.Relative),
            new StringContent("""{"username":"USER1","password":"PASSWORD1"}""", Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
    }

    private static async Task<HttpResponseMessage> SendWithTokenAsync(HttpClient http, HttpMethod method, Uri uri, string token)
    {
        using var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await http.SendAsync(request);
    }
}
