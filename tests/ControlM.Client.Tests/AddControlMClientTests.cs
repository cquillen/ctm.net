using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ControlM.Client.Tests;

public sealed class AddControlMClientTests
{
    [Test]
    public void A_missing_endpoint_fails_when_the_client_is_first_resolved()
    {
        var services = new ServiceCollection();
        services.AddControlMClient(o => o);
        using var provider = services.BuildServiceProvider();

        var exception = Should.Throw<InvalidOperationException>(provider.GetRequiredService<IControlMClient>);

        exception.Message.ShouldContain(nameof(ControlMClientOptions.Endpoint));
    }
}
