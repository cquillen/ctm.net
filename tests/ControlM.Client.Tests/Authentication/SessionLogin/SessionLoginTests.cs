using System.Net;
using System.Text.Json;
using ControlM.Fixtures;
using Shouldly;

namespace ControlM.Client.Tests.Authentication.SessionLogin;

public sealed class SessionLoginTests
{
    private const string LoginPath = "/automation-api/session/login";

    [Test]
    public void Resolving_the_client_sends_nothing()
    {
        var stub = StubHandler.Returning(HttpStatusCode.OK, "[]");

        TestClient.CreateWithSessionLogin(stub);

        stub.Requests.ShouldBeEmpty();
    }

    [Test]
    public async Task The_first_call_logs_in_through_the_primary_handler_then_sends_the_token_as_a_bearer_credential()
    {
        var stub = StubHandler.Returning(HttpStatusCode.OK, "[]").Answering(LoginPath, HttpStatusCode.OK, LoginSuccess);
        var client = TestClient.CreateWithSessionLogin(stub);

        await client.SendAsync<JsonElement>(HttpMethod.Get, "/config/servers");

        stub.Requests.Count.ShouldBe(2);
        var login = stub.Requests[0];
        login.Method.ShouldBe(HttpMethod.Post);
        login.Uri.AbsoluteUri.ShouldBe("https://host1.test:8443/automation-api/session/login");
        login.Body.ShouldEqualJson("""{"username":"USER1","password":"PASSWORD1"}""");
        login.Headers.ShouldNotContainKey("Authorization");
        login.Headers.ShouldNotContainKey("x-api-key");
        var call = stub.Requests[1];
        call.Uri.AbsoluteUri.ShouldBe("https://host1.test:8443/automation-api/config/servers");
        call.Headers.GetValueOrDefault("Authorization").ShouldBe(["Bearer SIMTOKEN1"]);
    }

    private static string LoginSuccess => JsonFixture.Load("Authentication", "Login", "Login.success.json");
}
