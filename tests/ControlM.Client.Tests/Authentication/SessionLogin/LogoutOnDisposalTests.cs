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
    public async Task Disposing_the_provider_after_a_login_sends_logout_through_the_primary_handler_with_the_bearer_token()
    {
        var stub = LoggedInStub()
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

    [Test]
    public async Task Disposing_the_provider_without_a_login_sends_nothing()
    {
        var stub = StubHandler.Returning(HttpStatusCode.OK, "[]");
        var provider = TestClient.BuildWithSessionLogin(stub);
        provider.GetRequiredService<IControlMClient>();

        await provider.DisposeAsync();

        stub.Requests.ShouldBeEmpty();
    }

    [Test]
    public async Task Disposing_the_provider_synchronously_after_a_login_does_not_throw_and_sends_no_logout()
    {
        var stub = LoggedInStub()
            .Answering(_logoutPath, HttpStatusCode.OK, Fixture("Logout", "Logout.success.json"));
        var provider = TestClient.BuildWithSessionLogin(stub);
        await provider.GetRequiredService<IControlMClient>().SendAsync<JsonElement>(HttpMethod.Get, "/config/servers");

        Should.NotThrow(provider.Dispose);

        stub.Requests.Select(r => r.Uri.AbsolutePath).ShouldBe([_loginPath, "/automation-api/config/servers"]);
    }

    [Test]
    public void Disposing_the_provider_synchronously_without_a_call_does_not_throw()
    {
        var stub = StubHandler.Returning(HttpStatusCode.OK, "[]");
        var provider = TestClient.BuildWithSessionLogin(stub);
        provider.GetRequiredService<IControlMClient>();

        Should.NotThrow(provider.Dispose);

        stub.Requests.ShouldBeEmpty();
    }

    [Test]
    [Arguments(HttpStatusCode.Unauthorized, """{"errors":[{"message":"User not found. Session token is invalid or expired."}]}""")]
    [Arguments(HttpStatusCode.InternalServerError, """{"errors":[{"message":"Internal error"}]}""")]
    public async Task A_logout_answered_with_an_error_is_swallowed_and_makes_no_login(HttpStatusCode status, string body)
    {
        var stub = LoggedInStub().Answering(_logoutPath, status, body);
        var provider = TestClient.BuildWithSessionLogin(stub);
        await provider.GetRequiredService<IControlMClient>().SendAsync<JsonElement>(HttpMethod.Get, "/config/servers");

        await Should.NotThrowAsync(provider.DisposeAsync().AsTask());

        stub.Requests.Select(r => r.Uri.AbsolutePath).ShouldBe([_loginPath, "/automation-api/config/servers", _logoutPath]);
    }

    [Test]
    public async Task A_logout_failing_with_a_network_error_is_swallowed_and_makes_no_login()
    {
        var stub = LoggedInStub().Failing(_logoutPath);
        var provider = TestClient.BuildWithSessionLogin(stub);
        await provider.GetRequiredService<IControlMClient>().SendAsync<JsonElement>(HttpMethod.Get, "/config/servers");

        await Should.NotThrowAsync(provider.DisposeAsync().AsTask());

        stub.Requests.Select(r => r.Uri.AbsolutePath).ShouldBe([_loginPath, "/automation-api/config/servers", _logoutPath]);
    }

    [Test]
    [Timeout(10_000)]
    public async Task Disposing_the_provider_during_a_login_waits_for_it_then_logs_out_its_session(CancellationToken testTimeout)
    {
        var stub = LoggedInStub().Answering(_logoutPath, HttpStatusCode.OK, Fixture("Logout", "Logout.success.json"));
        var hanging = new HangingHandler(stub);
        var provider = TestClient.BuildWithSessionLogin(hanging);
        hanging.Hold(_loginPath);
        var call = provider.GetRequiredService<IControlMClient>()
            .SendAsync(HttpMethod.Get, "/config/servers", cancellationToken: testTimeout);
        await hanging.Entered.WaitAsync(testTimeout);

        var disposal = provider.DisposeAsync().AsTask();
        hanging.Release();

        await Should.NotThrowAsync(disposal.WaitAsync(testTimeout));
        await Should.NotThrowAsync(call);
        stub.Requests.Where(r => r.Uri.AbsolutePath == _logoutPath)
            .ShouldHaveSingleItem().Headers.GetValueOrDefault("Authorization").ShouldBe(["Bearer SIMTOKEN1"]);
    }

    private static StubHandler LoggedInStub() =>
        StubHandler.Returning(HttpStatusCode.OK, "[]")
            .Answering(_loginPath, HttpStatusCode.OK, Fixture("Login", "Login.success.json"));

    private static string Fixture(string feature, string fileName) => JsonFixture.Load("Authentication", feature, fileName);
}
