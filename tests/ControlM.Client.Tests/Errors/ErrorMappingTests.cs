using System.Net;
using System.Text.Json;
using ControlM.Client.Errors;
using ControlM.Fixtures;
using Shouldly;

namespace ControlM.Client.Tests.Errors;

public sealed class ErrorMappingTests
{
    [Test]
    public async Task A_404_with_an_error_list_throws_not_found_with_its_entry()
    {
        var stub = StubHandler.Returning(
            HttpStatusCode.NotFound,
            JsonFixture.Load("Operations", "UnknownOperation", "UnknownOperation.get.json"));
        var client = TestClient.Create(stub);

        var exception = await Should.ThrowAsync<ControlMNotFoundException>(
            () => client.SendAsync<JsonElement>(HttpMethod.Get, "/no/such/path"));

        exception.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        exception.Errors.ShouldHaveSingleItem().ShouldBe(new ControlMError("Not Found", Code: "404", Uri: "/no/such/path"));
    }

    // The unknown API key fixture is assumed (requirements section 4.8); the others were observed.
    [Test]
    [Arguments("GetServers.no-credential.json", "Unauthenticated request: No authentication header was provided")]
    [Arguments("GetServers.invalid-bearer-token.json", "User not found. Session token is invalid or expired.")]
    [Arguments(
        "GetServers.both-headers.json",
        "Unauthenticated request: Request cannot contain both x-api-key and authorization headers")]
    [Arguments("GetServers.unknown-api-key.json", "Invalid API token.")]
    public async Task A_401_throws_the_authentication_exception_with_its_entry(string fixture, string message)
    {
        var stub = StubHandler.Returning(HttpStatusCode.Unauthorized, JsonFixture.Load("Authentication", "ApiKey", fixture));
        var client = TestClient.Create(stub);

        var exception = await Should.ThrowAsync<ControlMAuthenticationException>(
            () => client.SendAsync<JsonElement>(HttpMethod.Get, "/config/servers"));

        exception.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        exception.Errors.ShouldHaveSingleItem().ShouldBe(new ControlMError(message));
    }

    [Test]
    public async Task A_405_throws_the_api_exception_with_its_entry()
    {
        var stub = StubHandler.Returning(
            HttpStatusCode.MethodNotAllowed,
            JsonFixture.Load("Operations", "UnknownOperation", "UnknownOperation.wrong-method.json"));
        var client = TestClient.Create(stub);

        var exception = await Should.ThrowAsync<ControlMApiException>(
            () => client.SendAsync<JsonElement>(HttpMethod.Patch, "/config/servers"));

        exception.ShouldBeOfType<ControlMApiException>();
        exception.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
        exception.Errors.ShouldHaveSingleItem().ShouldBe(
            new ControlMError("Method Not Allowed", Code: "405", Uri: "/config/servers"));
    }

    [Test]
    public async Task Another_failure_status_throws_the_api_exception_with_every_entry()
    {
        var stub = StubHandler.Returning(
            HttpStatusCode.BadRequest,
            """
            {"errors":[
              {"message":"Job name is missing","id":"ERR1","item":"FOLDER1","file":"jobs.json","line":3,"col":7},
              {"message":"Unknown host","item":"HOST1","newerField":true}
            ]}
            """);
        var client = TestClient.Create(stub);

        var exception = await Should.ThrowAsync<ControlMApiException>(
            () => client.SendAsync<JsonElement>(HttpMethod.Post, "/sample/path"));

        exception.ShouldNotBeOfType<ControlMNotFoundException>();
        exception.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        exception.Errors.ShouldBe(
        [
            new ControlMError("Job name is missing", Id: "ERR1", Item: "FOLDER1", File: "jobs.json", Line: 3, Col: 7),
            new ControlMError("Unknown host", Item: "HOST1"),
        ]);
    }

    [Test]
    public async Task An_entry_with_every_field_reaches_the_error_with_every_field()
    {
        var stub = StubHandler.Returning(
            HttpStatusCode.BadRequest,
            """
            {"errors":[{"message":"Job name is missing","id":"ERR1","item":"FOLDER1","file":"jobs.json",
              "line":3,"col":7,"code":"400","uri":"/sample/path"}]}
            """);
        var client = TestClient.Create(stub);

        var exception = await Should.ThrowAsync<ControlMApiException>(
            () => client.SendAsync<JsonElement>(HttpMethod.Post, "/sample/path"));

        exception.Errors.ShouldHaveSingleItem().ShouldBe(new ControlMError(
            "Job name is missing",
            Id: "ERR1",
            Item: "FOLDER1",
            File: "jobs.json",
            Line: 3,
            Col: 7,
            Code: "400",
            Uri: "/sample/path"));
    }

    [Test]
    [Arguments("<html><body><h1>502 Bad Gateway</h1></body></html>")]
    [Arguments("")]
    [Arguments("""{"error":"Bad Gateway"}""")]
    [Arguments("""{"errors":[{"code":"502"}]}""")]
    public async Task A_failure_body_that_is_not_an_error_list_gives_no_entries_and_the_body_text(string body)
    {
        var stub = StubHandler.Returning(HttpStatusCode.BadGateway, body);
        var client = TestClient.Create(stub);

        var exception = await Should.ThrowAsync<ControlMApiException>(
            () => client.SendAsync<JsonElement>(HttpMethod.Get, "/sample/path"));

        exception.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
        exception.Errors.ShouldBeEmpty();
        exception.ResponseBody.ShouldBe(body);
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task The_exception_carries_the_method_and_path_of_the_call_and_no_operation(bool withResult)
    {
        var stub = StubHandler.Returning(
            HttpStatusCode.NotFound,
            JsonFixture.Load("Operations", "UnknownOperation", "UnknownOperation.get.json"));
        var client = TestClient.Create(stub);

        Func<Task> send = withResult
            ? () => client.SendAsync<JsonElement>(HttpMethod.Delete, "/sample/path?name=FOLDER1")
            : () => client.SendAsync(HttpMethod.Delete, "/sample/path?name=FOLDER1");
        var exception = await Should.ThrowAsync<ControlMApiException>(send);

        exception.Method.ShouldBe(HttpMethod.Delete);
        exception.Path.ShouldBe("/sample/path?name=FOLDER1");
        exception.Operation.ShouldBeNull();
    }

    [Test]
    public async Task The_exception_message_includes_the_status_code_and_the_first_error_message()
    {
        var stub = StubHandler.Returning(
            HttpStatusCode.Conflict,
            """{"errors":[{"message":"Folder FOLDER1 is locked"},{"message":"Second error"}]}""");
        var client = TestClient.Create(stub);

        var exception = await Should.ThrowAsync<ControlMApiException>(
            () => client.SendAsync(HttpMethod.Post, "/sample/path"));

        exception.Message.ShouldContain("409");
        exception.Message.ShouldContain("Folder FOLDER1 is locked");
    }
}
