using ControlM.Client.Operations;
using Shouldly;

namespace ControlM.Client.Tests.Operations;

public sealed class OperationTableTests
{
    [Test]
    public void Get_servers_is_a_get_of_config_servers()
    {
        Operation.GetServers.ShouldBe(new Operation("GetServers", HttpMethod.Get, "/config/servers"));
    }
}
