using System.Net;
using System.Text;
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

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    private static string LoginFixture(string fileName) => JsonFixture.Load("Authentication", "Login", fileName);
}
