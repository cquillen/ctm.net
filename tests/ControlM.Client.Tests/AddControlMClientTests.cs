using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ControlM.Client.Tests;

public sealed class AddControlMClientTests
{
    [Test]
    public void A_missing_endpoint_fails_when_the_client_is_first_resolved()
    {
        var services = new ServiceCollection();
        services.AddControlMClient(o => o.UseApiKey(TestClient.ApiKey));
        using var provider = services.BuildServiceProvider();

        var exception = Should.Throw<OptionsValidationException>(provider.GetRequiredService<IControlMClient>);

        exception.Message.ShouldContain(nameof(ControlMClientOptions.Endpoint));
    }

    [Test]
    public void A_missing_authentication_scheme_fails_when_the_client_is_first_resolved()
    {
        var services = new ServiceCollection();
        services.AddControlMClient(o => o.Endpoint = new Uri(TestClient.Endpoint));
        using var provider = services.BuildServiceProvider();

        var exception = Should.Throw<OptionsValidationException>(provider.GetRequiredService<IControlMClient>);

        exception.Message.ShouldContain("UseApiKey");
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments(" ")]
    public void A_blank_api_key_fails_when_the_client_is_first_resolved(string? key)
    {
        var services = new ServiceCollection();
        services.AddControlMClient(o =>
        {
            o.Endpoint = new Uri(TestClient.Endpoint);
            o.UseApiKey(key!);
        });
        using var provider = services.BuildServiceProvider();

        var exception = Should.Throw<OptionsValidationException>(provider.GetRequiredService<IControlMClient>);

        exception.Message.ShouldContain("UseApiKey");
    }
}
