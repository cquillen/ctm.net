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

    [Test]
    public void Login_is_a_post_of_session_login()
    {
        Operation.Login.ShouldBe(new Operation("Login", HttpMethod.Post, "/session/login"));
    }

    [Test]
    public void Logout_is_a_post_of_session_logout()
    {
        Operation.Logout.ShouldBe(new Operation("Logout", HttpMethod.Post, "/session/logout"));
    }
}
