using ControlM.Client.Authentication;
using ControlM.Client.Authentication.SessionLogin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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
                o => o.ApiKey is not null || o.SessionLogin is not null,
                $"An authentication scheme is required: call {nameof(ControlMClientOptions)}.{nameof(ControlMClientOptions.UseApiKey)} or {nameof(ControlMClientOptions)}.{nameof(ControlMClientOptions.UseSessionLogin)}.")
            .Validate(
                o => o.ApiKey is null || o.SessionLogin is null,
                $"Choose one authentication scheme: call {nameof(ControlMClientOptions)}.{nameof(ControlMClientOptions.UseApiKey)} or {nameof(ControlMClientOptions)}.{nameof(ControlMClientOptions.UseSessionLogin)}, not both.")
            .Validate(
                o => o.ApiKey is not { IsBlank: true },
                $"The key given to {nameof(ControlMClientOptions)}.{nameof(ControlMClientOptions.UseApiKey)} must not be empty or whitespace.")
            .Validate(
                o => o.SessionLogin is not { } login || !(string.IsNullOrWhiteSpace(login.Username) || string.IsNullOrWhiteSpace(login.Password)),
                $"The user name and password given to {nameof(ControlMClientOptions)}.{nameof(ControlMClientOptions.UseSessionLogin)} must not be empty or whitespace.");

        // A singleton, for the reason in docs/requirements.md section 3.4.
        services.TryAddSingleton(CredentialSchemeOf);
        return ControlMPipeline.Compose(services.AddHttpClient<IControlMClient, ControlMClient>());
    }

    private static ICredentialScheme CredentialSchemeOf(IServiceProvider provider) =>
        provider.GetRequiredService<IOptions<ControlMClientOptions>>().Value.ApiKey is { } apiKey
            ? apiKey
            : new SessionLoginScheme();
}
