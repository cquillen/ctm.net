using System.Net;
using System.Text.Json;
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
}
