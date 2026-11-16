using System.Net;
using System.Text.Json;
using Shouldly;

namespace ControlM.Client.Tests.Authentication.ApiKey;

public sealed class ApiKeyTests
{
    [Test]
    public async Task The_configured_key_is_sent_in_the_x_api_key_header()
    {
        var stub = StubHandler.Returning(HttpStatusCode.OK, "[]");
        var client = TestClient.Create(stub);

        await client.SendAsync<JsonElement>(HttpMethod.Get, "/config/servers");

        stub.Requests.ShouldHaveSingleItem().Headers.GetValueOrDefault("x-api-key").ShouldBe([TestClient.ApiKey]);
    }
}
