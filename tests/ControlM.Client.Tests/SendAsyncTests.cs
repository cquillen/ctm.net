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
    public async Task A_successful_response_is_deserialized_and_its_unknown_properties_are_ignored()
    {
        var stub = StubHandler.Returning(
            HttpStatusCode.OK,
            """{"name":"SERVER1","state":"Up","addedInLaterVersion":{"anything":1}}""");
        var client = TestClient.Create(stub);

        var server = await client.SendAsync<SampleServer>(HttpMethod.Get, "/sample/server");

        server.ShouldBe(new SampleServer("SERVER1", "Up"));
    }

    internal sealed record SampleServer(string Name, string State);

    internal sealed record SampleRequest(string FolderName, int RetryCount, string? Description);
}
