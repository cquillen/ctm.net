using System.Net;
using System.Text.Json;
using ControlM.Client.Errors;
using ControlM.Client.Operations;
using ControlM.Fixtures;
using Shouldly;

namespace ControlM.Client.Tests.Authentication.SessionLogin;

public sealed class SessionLoginTests
{
    private const string _loginPath = "/automation-api/session/login";

    private const string _serversPath = "/automation-api/config/servers";

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
        var stub = StubHandler.Returning(HttpStatusCode.OK, "[]").Answering(_loginPath, HttpStatusCode.OK, LoginSuccess);
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
        var stub = StubHandler.Returning(HttpStatusCode.OK, "[]").Answering(_loginPath, HttpStatusCode.OK, LoginSuccess);
        var client = TestClient.CreateWithSessionLogin(stub);

        await client.SendAsync<JsonElement>(HttpMethod.Get, "/config/servers");
        await client.SendAsync(HttpMethod.Post, "/sample/path");

        stub.Requests.Count(r => r.Uri.AbsolutePath == _loginPath).ShouldBe(1);
        stub.Requests[^1].Headers.GetValueOrDefault("Authorization").ShouldBe(["Bearer SIMTOKEN1"]);
    }

    [Test]
    public async Task A_login_answered_401_throws_the_authentication_exception_for_the_login_and_sends_nothing_more()
    {
        var stub = StubHandler.Returning(HttpStatusCode.OK, "[]")
            .Answering(_loginPath, HttpStatusCode.Unauthorized, LoginFixture("Login.unknown-user.json"));
        var client = TestClient.CreateWithSessionLogin(stub);

        var exception = await Should.ThrowAsync<ControlMAuthenticationException>(
            () => client.SendAsync<JsonElement>(HttpMethod.Get, "/config/servers"));

        exception.Operation.ShouldBe(Operation.Login);
        exception.Method.ShouldBe(HttpMethod.Post);
        exception.Path.ShouldBe("/session/login");
        exception.Errors.ShouldHaveSingleItem().ShouldBe(new ControlMError("Failed to login: Incorrect username or password"));
        stub.Requests.ShouldHaveSingleItem().Uri.AbsolutePath.ShouldBe(_loginPath);
    }

    [Test]
    public async Task A_login_answered_500_throws_the_api_exception_for_the_login_and_sends_nothing_more()
    {
        var stub = StubHandler.Returning(HttpStatusCode.OK, "[]")
            .Answering(_loginPath, HttpStatusCode.InternalServerError, LoginFixture("Login.wrong-password.json"));
        var client = TestClient.CreateWithSessionLogin(stub);

        var exception = await Should.ThrowAsync<ControlMApiException>(
            () => client.SendAsync<JsonElement>(HttpMethod.Get, "/config/servers"));

        exception.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        exception.Operation.ShouldBe(Operation.Login);
        exception.Path.ShouldBe("/session/login");
        exception.Errors.ShouldHaveSingleItem().ShouldBe(new ControlMError("Failed to login: Login failed."));
        stub.Requests.ShouldHaveSingleItem().Uri.AbsolutePath.ShouldBe(_loginPath);
    }

    [Test]
    public async Task A_call_answered_401_logs_in_again_once_and_resends_with_the_new_token()
    {
        var stub = StubHandler.Returning(HttpStatusCode.OK, "[]")
            .AnsweringInTurn(_loginPath, (HttpStatusCode.OK, LoginSuccess), (HttpStatusCode.OK, SecondLoginSuccess))
            .AnsweringInTurn(_serversPath, (HttpStatusCode.Unauthorized, InvalidToken), (HttpStatusCode.OK, """["SERVER1"]"""));
        var client = TestClient.CreateWithSessionLogin(stub);

        var servers = await client.SendAsync<JsonElement>(HttpMethod.Get, "/config/servers");

        servers.GetRawText().ShouldEqualJson("""["SERVER1"]""");
        stub.Requests.Select(r => r.Uri.AbsolutePath).ShouldBe([_loginPath, _serversPath, _loginPath, _serversPath]);
        stub.Requests[1].Headers.GetValueOrDefault("Authorization").ShouldBe(["Bearer SIMTOKEN1"]);
        stub.Requests[3].Headers.GetValueOrDefault("Authorization").ShouldBe(["Bearer SIMTOKEN2"]);
    }

    [Test]
    public async Task A_resend_answered_401_throws_the_authentication_exception_for_the_call_and_sends_nothing_more()
    {
        var stub = StubHandler.Returning(HttpStatusCode.Unauthorized, InvalidToken)
            .AnsweringInTurn(_loginPath, (HttpStatusCode.OK, LoginSuccess), (HttpStatusCode.OK, SecondLoginSuccess));
        var client = TestClient.CreateWithSessionLogin(stub);

        var exception = await Should.ThrowAsync<ControlMAuthenticationException>(
            () => client.SendAsync<JsonElement>(HttpMethod.Get, "/config/servers"));

        exception.Operation.ShouldBeNull();
        exception.Method.ShouldBe(HttpMethod.Get);
        exception.Path.ShouldBe("/config/servers");
        exception.Errors.ShouldHaveSingleItem().ShouldBe(new ControlMError("User not found. Session token is invalid or expired."));
        stub.Requests.Select(r => r.Uri.AbsolutePath).ShouldBe([_loginPath, _serversPath, _loginPath, _serversPath]);
    }

    [Test]
    [Timeout(10_000)]
    public async Task A_call_refused_for_a_token_already_replaced_resends_with_the_current_token_without_logging_in(
        CancellationToken testTimeout)
    {
        const string slowPath = "/automation-api/sample/slow";
        var stub = StubHandler.Returning(HttpStatusCode.OK, "[]")
            .AnsweringInTurn(_loginPath, (HttpStatusCode.OK, LoginSuccess), (HttpStatusCode.OK, SecondLoginSuccess))
            .AnsweringInTurn(_serversPath, (HttpStatusCode.OK, "[]"), (HttpStatusCode.Unauthorized, InvalidToken), (HttpStatusCode.OK, "[]"))
            .AnsweringInTurn(slowPath, (HttpStatusCode.Unauthorized, InvalidToken), (HttpStatusCode.OK, "[]"));
        var hanging = new HangingHandler(stub);
        var client = TestClient.CreateWithSessionLogin(hanging);
        await client.SendAsync(HttpMethod.Get, "/config/servers", cancellationToken: testTimeout);
        hanging.Hold(slowPath);
        var slow = client.SendAsync(HttpMethod.Get, "/sample/slow", cancellationToken: testTimeout);
        await hanging.Entered.WaitAsync(testTimeout);

        await client.SendAsync(HttpMethod.Get, "/config/servers", cancellationToken: testTimeout);
        hanging.Release();
        await slow;

        stub.Requests.Count(r => r.Uri.AbsolutePath == _loginPath).ShouldBe(2);
        var slowCalls = stub.Requests.Where(r => r.Uri.AbsolutePath == slowPath).ToList();
        slowCalls.Select(r => r.Headers["Authorization"].Single()).ShouldBe(["Bearer SIMTOKEN1", "Bearer SIMTOKEN2"]);
    }

    [Test]
    [Timeout(10_000)]
    public async Task Calls_answered_401_together_share_one_login(CancellationToken testTimeout)
    {
        var stub = StubHandler.Returning(HttpStatusCode.OK, "[]")
            .AnsweringInTurn(_loginPath, (HttpStatusCode.OK, LoginSuccess), (HttpStatusCode.OK, SecondLoginSuccess))
            .AnsweringInTurn(
                _serversPath,
                (HttpStatusCode.OK, "[]"),
                (HttpStatusCode.Unauthorized, InvalidToken),
                (HttpStatusCode.Unauthorized, InvalidToken),
                (HttpStatusCode.OK, "[]"));
        var hanging = new HangingHandler(stub);
        var client = TestClient.CreateWithSessionLogin(hanging);
        await client.SendAsync(HttpMethod.Get, "/config/servers", cancellationToken: testTimeout);
        hanging.Hold(_loginPath);
        var refusals = hanging.CountAnswers(_serversPath, 2);

        var first = client.SendAsync(HttpMethod.Get, "/config/servers", cancellationToken: testTimeout);
        var second = client.SendAsync(HttpMethod.Get, "/config/servers", cancellationToken: testTimeout);
        await hanging.Entered.WaitAsync(testTimeout);
        await refusals.WaitAsync(testTimeout);
        hanging.Release();
        await Task.WhenAll(first, second);

        stub.Requests.Count(r => r.Uri.AbsolutePath == _loginPath).ShouldBe(2);
        stub.Requests[^2..].Select(r => r.Headers["Authorization"].Single()).ShouldBe(["Bearer SIMTOKEN2", "Bearer SIMTOKEN2"]);
    }

    [Test]
    [Timeout(10_000)]
    public async Task First_calls_made_together_share_one_login(CancellationToken testTimeout)
    {
        var stub = StubHandler.Returning(HttpStatusCode.OK, "[]")
            .AnsweringInTurn(_loginPath, (HttpStatusCode.OK, LoginSuccess), (HttpStatusCode.OK, SecondLoginSuccess));
        var hanging = new HangingHandler(stub);
        var client = TestClient.CreateWithSessionLogin(hanging);
        hanging.Hold(_loginPath);

        var first = client.SendAsync(HttpMethod.Get, "/config/servers", cancellationToken: testTimeout);
        var second = client.SendAsync(HttpMethod.Get, "/config/servers", cancellationToken: testTimeout);
        await hanging.Entered.WaitAsync(testTimeout);
        hanging.Release();
        await Task.WhenAll(first, second);

        stub.Requests.Count(r => r.Uri.AbsolutePath == _loginPath).ShouldBe(1);
        stub.Requests[^2..].Select(r => r.Headers["Authorization"].Single()).ShouldBe(["Bearer SIMTOKEN1", "Bearer SIMTOKEN1"]);
    }

    [Test]
    public async Task A_resent_call_carries_the_body_bytes_of_the_first_send()
    {
        const string samplePath = "/automation-api/sample/path";
        var stub = StubHandler.Returning(HttpStatusCode.OK, "[]")
            .AnsweringInTurn(_loginPath, (HttpStatusCode.OK, LoginSuccess), (HttpStatusCode.OK, SecondLoginSuccess))
            .AnsweringInTurn(samplePath, (HttpStatusCode.Unauthorized, InvalidToken), (HttpStatusCode.OK, "{}"));
        var client = TestClient.CreateWithSessionLogin(stub);

        await client.SendAsync(HttpMethod.Post, "/sample/path", new SerializationCounter());

        var sent = stub.Requests.Where(r => r.Uri.AbsolutePath == samplePath).Select(r => r.Body).ToList();
        sent.Count.ShouldBe(2);
        sent[0].ShouldEqualJson("""{"serializations":1}""");
        sent[1].ShouldEqualJson("""{"serializations":1}""");
    }

    [Test]
    public async Task A_failed_re_login_throws_for_the_login_and_sends_nothing_more()
    {
        var stub = StubHandler.Returning(HttpStatusCode.Unauthorized, InvalidToken)
            .AnsweringInTurn(
                _loginPath,
                (HttpStatusCode.OK, LoginSuccess),
                (HttpStatusCode.InternalServerError, LoginFixture("Login.wrong-password.json")));
        var client = TestClient.CreateWithSessionLogin(stub);

        var exception = await Should.ThrowAsync<ControlMApiException>(
            () => client.SendAsync<JsonElement>(HttpMethod.Get, "/config/servers"));

        exception.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        exception.Operation.ShouldBe(Operation.Login);
        exception.Path.ShouldBe("/session/login");
        stub.Requests.Select(r => r.Uri.AbsolutePath).ShouldBe([_loginPath, _serversPath, _loginPath]);
    }

    private static string LoginSuccess => LoginFixture("Login.success.json");

    private static string SecondLoginSuccess => LoginSuccess.Replace("SIMTOKEN1", "SIMTOKEN2", StringComparison.Ordinal);

    private static string InvalidToken => JsonFixture.Load("Authentication", "ApiKey", "GetServers.invalid-bearer-token.json");

    private static string LoginFixture(string fileName) => JsonFixture.Load("Authentication", "Login", fileName);

    // Serializes differently each time, so a resend shows whether the body was serialized again.
    internal sealed class SerializationCounter
    {
        private int _serializations;

        public int Serializations => ++_serializations;
    }
}
