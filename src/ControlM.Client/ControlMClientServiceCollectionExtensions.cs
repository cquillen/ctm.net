using Microsoft.Extensions.DependencyInjection;

namespace ControlM.Client;

/// <summary>
/// Registers <see cref="IControlMClient"/> with a service collection.
/// </summary>
public static class ControlMClientServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IControlMClient"/> over <see cref="IHttpClientFactory"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">
    /// Sets the options: <c>o =&gt; o.Endpoint = new Uri("https://host1:8443/automation-api")</c>.
    /// </param>
    /// <returns>The builder of the underlying HTTP client, to set its primary handler or add handlers of your own.</returns>
    public static IHttpClientBuilder AddControlMClient(
        this IServiceCollection services,
        Action<ControlMClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddSingleton(_ =>
        {
            var options = new ControlMClientOptions();
            configure(options);
            return options;
        });
        return ControlMPipeline.Compose(services.AddHttpClient<IControlMClient, ControlMClient>());
    }
}
