using System.Net;
using ControlM.Client.Errors;
using ControlM.Simulator.Hosting;
using Shouldly;

namespace ControlM.Conformance.Tests.Hosting;

public sealed class ControlMSimulatorTests
{
    [Test]
    public async Task A_client_keeps_reaching_its_own_instance_after_another_is_disposed()
    {
        await using var survivor = await ControlMSimulator.StartAsync();
        var disposed = await ControlMSimulator.StartAsync();
        disposed.CreateClient();
        var survivorClient = survivor.CreateClient();
        await disposed.DisposeAsync();

        var exception = await Should.ThrowAsync<ControlMNotFoundException>(
            () => survivorClient.SendAsync(HttpMethod.Get, "/no/such/path"));

        exception.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
