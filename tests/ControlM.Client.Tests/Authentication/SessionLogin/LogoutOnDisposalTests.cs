using System.Net;
using System.Text.Json;
using ControlM.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ControlM.Client.Tests.Authentication.SessionLogin;

public sealed class LogoutOnDisposalTests
{
    private const string _loginPath = "/automation-api/session/login";

    private const string _logoutPath = "/automation-api/session/logout";

    [Test]
    public async Task Disposing_the_provider_after_a_login_sends_logout_with_the_bearer_token()
    {
        var stub = StubHandler.Returning(HttpStatusCode.OK, "[]")
            .Answering(_loginPath, HttpStatusCode.OK, Fixture("Login", "Login.success.json"))
            .Answering(_logoutPath, HttpStatusCode.OK, Fixture("Logout", "Logout.success.json"));
        var provider = TestClient.BuildWithSessionLogin(stub);
        await provider.GetRequiredService<IControlMClient>().SendAsync<JsonElement>(HttpMethod.Get, "/config/servers");

        await provider.DisposeAsync();

        stub.Requests.Count.ShouldBe(3);
        var logout = stub.Requests[^1];
        logout.Method.ShouldBe(HttpMethod.Post);
        logout.Uri.AbsoluteUri.ShouldBe("https://host1.test:8443/automation-api/session/logout");
        logout.Headers.GetValueOrDefault("Authorization").ShouldBe(["Bearer SIMTOKEN1"]);
        logout.Headers.ShouldNotContainKey("x-api-key");
    }

    private static string Fixture(string feature, string fileName) => JsonFixture.Load("Authentication", feature, fileName);
}
