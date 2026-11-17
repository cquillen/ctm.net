using System.Text.Json;
using ControlM.Client.Errors;
using ControlM.Client.Operations;
using ControlM.Simulator.Hosting;
using Shouldly;

namespace ControlM.Conformance.Tests.Authentication.SessionLogin;

public sealed class SessionLoginTests
{
    [Test]
    public async Task A_client_with_session_login_reads_the_servers_as_an_array()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        simulator.Credentials.AddUser("USER1", "PASSWORD1");
        var client = simulator.CreateClient(o => o.UseSessionLogin("USER1", "PASSWORD1"));

        var servers = await client.SendAsync<JsonElement>(Operation.GetServers.Method, Operation.GetServers.Path);

        servers.ValueKind.ShouldBe(JsonValueKind.Array);
    }

    [Test]
    [Category("SimulatorOnly")]
    public async Task A_client_logs_in_again_after_every_session_expires()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        simulator.Credentials.AddUser("USER1", "PASSWORD1");
        var client = simulator.CreateClient(o => o.UseSessionLogin("USER1", "PASSWORD1"));
        await client.SendAsync(Operation.GetServers.Method, Operation.GetServers.Path);

        simulator.Credentials.ExpireSessions();
        var servers = await client.SendAsync<JsonElement>(Operation.GetServers.Method, Operation.GetServers.Path);

        servers.ValueKind.ShouldBe(JsonValueKind.Array);
    }

    [Test]
    public async Task A_client_with_an_unknown_user_throws_the_authentication_exception_for_the_login()
    {
        await using var target = await ConformanceTarget.StartAsync(o => o.UseSessionLogin("USER1", "PASSWORD1"));

        var exception = await Should.ThrowAsync<ControlMAuthenticationException>(
            () => target.Client.SendAsync(Operation.GetServers.Method, Operation.GetServers.Path));

        exception.Operation.ShouldBe(Operation.Login);
    }
}
