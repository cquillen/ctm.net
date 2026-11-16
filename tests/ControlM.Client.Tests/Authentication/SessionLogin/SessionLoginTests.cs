using System.Net;
using System.Text.Json;
using ControlM.Client.Errors;
using ControlM.Client.Operations;
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

    [Test]
    public async Task Later_calls_reuse_the_token_without_logging_in_again()
    {
        var stub = StubHandler.Returning(HttpStatusCode.OK, "[]").Answering(LoginPath, HttpStatusCode.OK, LoginSuccess);
        var client = TestClient.CreateWithSessionLogin(stub);

        await client.SendAsync<JsonElement>(HttpMethod.Get, "/config/servers");
        await client.SendAsync(HttpMethod.Post, "/sample/path");

        stub.Requests.Count(r => r.Uri.AbsolutePath == LoginPath).ShouldBe(1);
        stub.Requests[^1].Headers.GetValueOrDefault("Authorization").ShouldBe(["Bearer SIMTOKEN1"]);
    }

    [Test]
    public async Task A_login_answered_401_throws_the_authentication_exception_for_the_login_and_sends_nothing_more()
    {
        var stub = StubHandler.Returning(HttpStatusCode.OK, "[]")
            .Answering(LoginPath, HttpStatusCode.Unauthorized, LoginFixture("Login.unknown-user.json"));
        var client = TestClient.CreateWithSessionLogin(stub);

        var exception = await Should.ThrowAsync<ControlMAuthenticationException>(
            () => client.SendAsync<JsonElement>(HttpMethod.Get, "/config/servers"));

        exception.Operation.ShouldBe(Operation.Login);
        exception.Method.ShouldBe(HttpMethod.Post);
        exception.Path.ShouldBe("/session/login");
        exception.Errors.ShouldHaveSingleItem().ShouldBe(new ControlMError("Failed to login: Incorrect username or password"));
        stub.Requests.ShouldHaveSingleItem().Uri.AbsolutePath.ShouldBe(LoginPath);
    }

    [Test]
    public async Task A_login_answered_500_throws_the_api_exception_for_the_login_and_sends_nothing_more()
    {
        var stub = StubHandler.Returning(HttpStatusCode.OK, "[]")
            .Answering(LoginPath, HttpStatusCode.InternalServerError, LoginFixture("Login.wrong-password.json"));
        var client = TestClient.CreateWithSessionLogin(stub);

        var exception = await Should.ThrowAsync<ControlMApiException>(
            () => client.SendAsync<JsonElement>(HttpMethod.Get, "/config/servers"));

        exception.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        exception.Operation.ShouldBe(Operation.Login);
        exception.Path.ShouldBe("/session/login");
        exception.Errors.ShouldHaveSingleItem().ShouldBe(new ControlMError("Failed to login: Login failed."));
        stub.Requests.ShouldHaveSingleItem().Uri.AbsolutePath.ShouldBe(LoginPath);
    }

    private static string LoginSuccess => LoginFixture("Login.success.json");

    private static string LoginFixture(string fileName) => JsonFixture.Load("Authentication", "Login", fileName);
}
