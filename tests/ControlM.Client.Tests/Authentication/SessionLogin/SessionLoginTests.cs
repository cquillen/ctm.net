using System.Net;
using Shouldly;

namespace ControlM.Client.Tests.Authentication.SessionLogin;

public sealed class SessionLoginTests
{
    [Test]
    public void Resolving_the_client_sends_nothing()
    {
        var stub = StubHandler.Returning(HttpStatusCode.OK, "[]");

        TestClient.CreateWithSessionLogin(stub);

        stub.Requests.ShouldBeEmpty();
    }
}
