using Microsoft.Extensions.DependencyInjection;

namespace ControlM.Client.Tests;

internal static class TestClient
{
    public const string Endpoint = "https://host1.test:8443/automation-api";

    public const string ApiKey = "APIKEY1";

    public const string Username = "USER1";

    public const string Password = "PASSWORD1";

    public static IControlMClient Create(HttpMessageHandler primaryHandler, string endpoint = Endpoint) =>
        Create(primaryHandler, endpoint, o => o.UseApiKey(ApiKey));

    public static IControlMClient CreateWithSessionLogin(HttpMessageHandler primaryHandler) =>
        Create(primaryHandler, Endpoint, o => o.UseSessionLogin(Username, Password));

    private static IControlMClient Create(
        HttpMessageHandler primaryHandler,
        string endpoint,
        Action<ControlMClientOptions> chooseScheme)
    {
        var services = new ServiceCollection();
        services.AddControlMClient(o =>
            {
                o.Endpoint = new Uri(endpoint);
                chooseScheme(o);
            })
            .ConfigurePrimaryHttpMessageHandler(() => primaryHandler);
        return services.BuildServiceProvider().GetRequiredService<IControlMClient>();
    }
}
