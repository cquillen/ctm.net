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
}
