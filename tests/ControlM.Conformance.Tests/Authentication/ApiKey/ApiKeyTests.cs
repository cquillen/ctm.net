using System.Net;
using ControlM.Client.Errors;
using ControlM.Client.Operations;
using ControlM.Simulator.Hosting;
using Shouldly;

namespace ControlM.Conformance.Tests.Authentication.ApiKey;

public sealed class ApiKeyTests
{
    [Test]
    [Category("SimulatorOnly")]
    public async Task A_client_with_an_unknown_key_throws_the_authentication_exception()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        var client = simulator.CreateClient(o => o.UseApiKey("APIKEY1"));

        var exception = await Should.ThrowAsync<ControlMAuthenticationException>(
            () => client.SendAsync(Operation.GetServers.Method, Operation.GetServers.Path));

        exception.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
