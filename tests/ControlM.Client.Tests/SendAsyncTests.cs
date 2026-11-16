using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Shouldly;

namespace ControlM.Client.Tests;

public sealed class SendAsyncTests
{
    [Test]
    public async Task A_request_goes_with_its_method_to_the_endpoint_plus_the_path_and_query()
    {
        var stub = StubHandler.Returning(HttpStatusCode.OK, "{}");
        var client = TestClient.Create(stub);

        await client.SendAsync<JsonElement>(HttpMethod.Put, "/sample/path?name=FOLDER1&limit=10");

        var request = stub.Requests.ShouldHaveSingleItem();
        request.Method.ShouldBe(HttpMethod.Put);
        request.Uri.AbsoluteUri.ShouldBe("https://host1.test:8443/automation-api/sample/path?name=FOLDER1&limit=10");
    }

    [Test]
    [Arguments("https://host1.test:8443/automation-api", "/sample/path")]
    [Arguments("https://host1.test:8443/automation-api/", "/sample/path")]
    [Arguments("https://host1.test:8443/automation-api", "sample/path")]
    [Arguments("https://host1.test:8443/automation-api/", "sample/path")]
    public async Task Slashes_between_the_endpoint_and_the_path_do_not_change_the_url(string endpoint, string path)
    {
        var stub = StubHandler.Returning(HttpStatusCode.OK, "{}");
        var client = TestClient.Create(stub, endpoint);

        await client.SendAsync<JsonElement>(HttpMethod.Get, path);

        stub.Requests.ShouldHaveSingleItem().Uri.AbsoluteUri.ShouldBe("https://host1.test:8443/automation-api/sample/path");
    }

    [Test]
    public async Task A_body_is_sent_as_json_with_camel_case_names_and_no_null_properties()
    {
        var stub = StubHandler.Returning(HttpStatusCode.OK, "{}");
        var client = TestClient.Create(stub);

        await client.SendAsync<JsonElement>(
            HttpMethod.Post,
            "/sample/path",
            new SampleRequest("FOLDER1", 3, Description: null));

        var sent = JsonNode.Parse(stub.Requests.ShouldHaveSingleItem().Body.ShouldNotBeNull());
        JsonNode.DeepEquals(sent, JsonNode.Parse("""{"folderName":"FOLDER1","retryCount":3}"""))
            .ShouldBeTrue($"sent {sent?.ToJsonString()}");
    }

    [Test]
    public async Task Without_a_body_no_content_is_sent()
    {
        var stub = StubHandler.Returning(HttpStatusCode.OK, "{}");
        var client = TestClient.Create(stub);

        await client.SendAsync<JsonElement>(HttpMethod.Post, "/sample/path");

        stub.Requests.ShouldHaveSingleItem().Body.ShouldBeNull();
    }

    [Test]
    public async Task A_successful_response_is_deserialized_and_its_unknown_properties_are_ignored()
    {
        var stub = StubHandler.Returning(
            HttpStatusCode.OK,
            """{"name":"SERVER1","state":"Up","addedInLaterVersion":{"anything":1}}""");
        var client = TestClient.Create(stub);

        var server = await client.SendAsync<SampleServer>(HttpMethod.Get, "/sample/server");

        server.ShouldBe(new SampleServer("SERVER1", "Up"));
    }

    [Test]
    public async Task The_form_without_a_result_sends_the_request_and_reads_no_body()
    {
        var stub = StubHandler.Returning(HttpStatusCode.OK, "this body is not JSON");
        var client = TestClient.Create(stub);

        await client.SendAsync(HttpMethod.Post, "/sample/path?name=FOLDER1", new SampleRequest("FOLDER1", 3, null));

        var request = stub.Requests.ShouldHaveSingleItem();
        request.Method.ShouldBe(HttpMethod.Post);
        request.Uri.AbsoluteUri.ShouldBe("https://host1.test:8443/automation-api/sample/path?name=FOLDER1");
        request.Body.ShouldNotBeNull();
    }

    [Test]
    [Arguments(null, "/sample/path", true)]
    [Arguments("GET", null, true)]
    [Arguments("GET", "", true)]
    [Arguments("GET", " ", true)]
    [Arguments(null, "/sample/path", false)]
    [Arguments("GET", null, false)]
    [Arguments("GET", "", false)]
    [Arguments("GET", " ", false)]
    public async Task A_missing_method_or_path_is_rejected_before_anything_is_sent(string? method, string? path, bool withResult)
    {
        var stub = StubHandler.Returning(HttpStatusCode.OK, "{}");
        var client = TestClient.Create(stub);
        var httpMethod = method is null ? null : new HttpMethod(method);

        Func<Task> send = withResult
            ? () => client.SendAsync<JsonElement>(httpMethod!, path!)
            : () => client.SendAsync(httpMethod!, path!);

        await Should.ThrowAsync<ArgumentException>(send);
        stub.Requests.ShouldBeEmpty();
    }

    [Test]
    [Timeout(10_000)]
    [Arguments(true)]
    [Arguments(false)]
    public async Task Cancelling_the_token_cancels_a_request_in_flight(bool withResult, CancellationToken testTimeout)
    {
        var handler = new HangingHandler();
        var client = TestClient.Create(handler);
        using var cancellation = new CancellationTokenSource();

        var send = withResult
            ? client.SendAsync<JsonElement>(HttpMethod.Get, "/sample/path", cancellationToken: cancellation.Token)
            : client.SendAsync(HttpMethod.Get, "/sample/path", cancellationToken: cancellation.Token);
        await handler.Entered.WaitAsync(testTimeout);
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(send);
    }

    internal sealed record SampleServer(string Name, string State);

    internal sealed record SampleRequest(string FolderName, int RetryCount, string? Description);
}
