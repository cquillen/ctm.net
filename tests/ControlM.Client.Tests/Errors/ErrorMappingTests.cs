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
}
