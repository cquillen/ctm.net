using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ControlM.Fixtures;
using ControlM.Simulator.Hosting;
using Shouldly;

namespace ControlM.Simulator.Tests.Authentication.Login;

public sealed class LoginTests
{
    private static Uri Login => new("/automation-api/session/login", UriKind.Relative);

    [Test]
    public async Task A_seeded_user_with_the_right_password_answers_200_with_a_token()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        simulator.Credentials.AddUser("USER1", "PASSWORD1");
        using var http = simulator.CreateHttpClient();

        using var response = await http.PostAsync(Login, Json("""{"username":"USER1","password":"PASSWORD1"}"""));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(LoginFixture("Login.success.json"));
    }

    [Test]
    [Arguments("""{"username":"USER2","password":"PASSWORD1"}""")]
    [Arguments("""{"password":"PASSWORD1"}""")]
    [Arguments("""{"username":"USER1"}""")]
    [Arguments("{}")]
    public async Task An_unknown_user_or_a_missing_field_answers_401_with_the_observed_body(string body)
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        simulator.Credentials.AddUser("USER1", "PASSWORD1");
        using var http = simulator.CreateHttpClient();

        using var response = await http.PostAsync(Login, Json(body));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(LoginFixture("Login.unknown-user.json"));
    }

    [Test]
    public async Task A_seeded_user_with_the_wrong_password_answers_500_with_the_observed_body()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        simulator.Credentials.AddUser("USER1", "PASSWORD1");
        using var http = simulator.CreateHttpClient();

        using var response = await http.PostAsync(Login, Json("""{"username":"USER1","password":"PASSWORD2"}"""));

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(LoginFixture("Login.wrong-password.json"));
    }

    // The 400 and its body are assumed (requirements section 4.8): no real server has been sent a body that is not JSON.
    [Test]
    [Arguments("this is not JSON")]
    [Arguments("")]
    [Arguments("""["USER1","PASSWORD1"]""")]
    public async Task A_body_that_is_not_a_json_object_answers_400_with_the_assumed_body(string body)
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        simulator.Credentials.AddUser("USER1", "PASSWORD1");
        using var http = simulator.CreateHttpClient();

        using var response = await http.PostAsync(Login, Json(body));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(LoginFixture("Login.not-json.json"));
    }

    // That login ignores credential headers is assumed (requirements section 4.8).
    [Test]
    [Arguments(true, false)]
    [Arguments(false, true)]
    [Arguments(true, true)]
    public async Task A_login_carrying_credential_headers_still_succeeds(bool withApiKey, bool withAuthorization)
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        simulator.Credentials.AddUser("USER1", "PASSWORD1");
        using var http = simulator.CreateHttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, Login)
        {
            Content = Json("""{"username":"USER1","password":"PASSWORD1"}"""),
        };
        if (withApiKey)
        {
            request.Headers.Add("x-api-key", "APIKEY1");
        }

        if (withAuthorization)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "TOKEN1");
        }

        using var response = await http.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(LoginFixture("Login.success.json"));
    }

    [Test]
    public async Task A_user_seeded_on_one_instance_cannot_log_in_to_another()
    {
        await using var seeded = await ControlMSimulator.StartAsync();
        await using var other = await ControlMSimulator.StartAsync();
        seeded.Credentials.AddUser("USER1", "PASSWORD1");
        using var http = other.CreateHttpClient();

        using var response = await http.PostAsync(Login, Json("""{"username":"USER1","password":"PASSWORD1"}"""));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(LoginFixture("Login.unknown-user.json"));
    }

    [Test]
    public async Task Tokens_are_numbered_from_one_on_each_instance()
    {
        await using var first = await ControlMSimulator.StartAsync();
        await using var second = await ControlMSimulator.StartAsync();
        first.Credentials.AddUser("USER1", "PASSWORD1");
        second.Credentials.AddUser("USER1", "PASSWORD1");
        using var firstHttp = first.CreateHttpClient();
        using var secondHttp = second.CreateHttpClient();

        string[] tokens =
        [
            await LogInAsync(firstHttp),
            await LogInAsync(firstHttp),
            await LogInAsync(secondHttp),
        ];

        tokens.ShouldBe(["SIMTOKEN1", "SIMTOKEN2", "SIMTOKEN1"]);
    }

    private static async Task<string> LogInAsync(HttpClient http)
    {
        using var response = await http.PostAsync(Login, Json("""{"username":"USER1","password":"PASSWORD1"}"""));
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("token").GetString()!;
    }

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    private static string LoginFixture(string fileName) => JsonFixture.Load("Authentication", "Login", fileName);
}
