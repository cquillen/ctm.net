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
    /// Sets the options and chooses the authentication scheme, which is required:
    /// <c>o =&gt; { o.Endpoint = new Uri("https://host1:8443/automation-api"); o.UseApiKey(key); }</c>.
    /// </param>
    /// <returns>The builder of the underlying HTTP client, to set its primary handler or add handlers of your own.</returns>
    public static IHttpClientBuilder AddControlMClient(
        this IServiceCollection services,
        Action<ControlMClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<ControlMClientOptions>()
            .Configure(configure)
            .Validate(
                o => o.Endpoint is not null,
                $"{nameof(ControlMClientOptions)}.{nameof(ControlMClientOptions.Endpoint)} is required.")
            .Validate(
                o => o.CredentialScheme is not null,
                $"An authentication scheme is required: call {nameof(ControlMClientOptions)}.{nameof(ControlMClientOptions.UseApiKey)}.");
        return ControlMPipeline.Compose(services.AddHttpClient<IControlMClient, ControlMClient>());
    }
}
