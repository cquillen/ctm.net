using Microsoft.Extensions.DependencyInjection;

namespace ControlM.Client.Tests;

internal static class TestClient
{
    public const string Endpoint = "https://host1.test:8443/automation-api";

    public static IControlMClient Create(HttpMessageHandler primaryHandler, string endpoint = Endpoint)
    {
        var services = new ServiceCollection();
        services.AddControlMClient(o => o with { Endpoint = new Uri(endpoint) })
            .ConfigurePrimaryHttpMessageHandler(() => primaryHandler);
        return services.BuildServiceProvider().GetRequiredService<IControlMClient>();
    }
}
