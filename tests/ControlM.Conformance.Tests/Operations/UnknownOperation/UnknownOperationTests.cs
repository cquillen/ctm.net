using System.Net;
using ControlM.Client;
using ControlM.Client.Errors;
using ControlM.Simulator.Hosting;
using Shouldly;

namespace ControlM.Conformance.Tests.Operations.UnknownOperation;

public sealed class UnknownOperationTests
{
    [Test]
    public async Task Sending_to_an_unknown_path_throws_not_found_with_the_server_error()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        IControlMClient client = simulator.CreateClient();

        var exception = await Should.ThrowAsync<ControlMNotFoundException>(
            () => client.SendAsync(HttpMethod.Get, "/no/such/path"));

        exception.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        exception.Errors.ShouldHaveSingleItem().Message.ShouldBe("Not Found");
    }
}
