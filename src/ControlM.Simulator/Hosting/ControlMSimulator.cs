using System.Collections.Concurrent;
using ControlM.Client;
using ControlM.Simulator.Authentication;
using ControlM.Simulator.Authentication.Login;
using ControlM.Simulator.Operations;
using ControlM.Simulator.Operations.UnknownOperation;
using ControlM.Simulator.Topology.GetServers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace ControlM.Simulator.Hosting;

/// <summary>
/// A simulator of the Control-M Automation API, running in process with no socket. Start one per test with
/// <see cref="StartAsync"/> and dispose it when the test ends; instances share no state.
/// </summary>
public sealed class ControlMSimulator : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly ConcurrentBag<ServiceProvider> _clientProviders = [];
    private readonly ApiKeySequence _apiKeys = new();

    private ControlMSimulator(WebApplication app, CredentialStore credentials)
    {
        _app = app;
        Credentials = credentials;
    }

    /// <summary>
    /// The credentials this instance accepts. Seed one here to send it over a raw <see cref="HttpClient"/>.
    /// </summary>
    public CredentialStore Credentials { get; }

    /// <summary>
    /// Starts a simulator instance.
    /// </summary>
    /// <param name="configure">Sets the options, such as <c>o =&gt; o.TimeProvider = time</c>, or <see langword="null"/> for the defaults.</param>
    /// <param name="cancellationToken">Cancels the start.</param>
    /// <returns>The started instance, which serves the Control-M API under <c>/automation-api</c>.</returns>
    public static async Task<ControlMSimulator> StartAsync(
        Action<ControlMSimulatorOptions>? configure = null,
        CancellationToken cancellationToken = default)
    {
        var options = new ControlMSimulatorOptions();
        configure?.Invoke(options);

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();
        var credentials = new CredentialStore();
        MapSessionOperations(app.MapGroup(ApiPath.Base), credentials);
        MapOperations(app.MapGroup(ApiPath.Base).AddEndpointFilter(new CredentialFilter(credentials)));
        UnknownOperationEndpoint.Map(app).AddEndpointFilter(new WrongMethodFilter(app.Services.GetRequiredService<EndpointDataSource>()));
        await app.StartAsync(cancellationToken);
        return new ControlMSimulator(app, credentials);
    }

    // The one registration of each implemented operation. The session operations check no credential of their own.
    private static void MapSessionOperations(IEndpointRouteBuilder api, CredentialStore credentials) =>
        LoginEndpoint.Map(api, credentials);

    private static void MapOperations(IEndpointRouteBuilder api) => GetServersEndpoint.Map(api);

    /// <summary>
    /// Returns a raw <see cref="HttpClient"/> whose base address is the root of the simulator, so a request
    /// names the whole path, such as <c>/automation-api/run/jobs/status</c>.
    /// </summary>
    public HttpClient CreateHttpClient() => _app.GetTestServer().CreateClient();

    /// <summary>
    /// Returns a real <see cref="IControlMClient"/> whose requests reach this instance under <c>/automation-api</c>,
    /// with no socket, authenticated with an API key the simulator generates and seeds: <c>SIMKEY1</c> for the first
    /// client of an instance, <c>SIMKEY2</c> for the second, and so on. The client is released when the simulator is
    /// disposed.
    /// </summary>
    public IControlMClient CreateClient()
    {
        var apiKey = _apiKeys.Next();
        Credentials.AddApiKey(apiKey);
        return CreateClient(o => o.UseApiKey(apiKey));
    }

    /// <summary>
    /// Returns a real <see cref="IControlMClient"/> whose requests reach this instance under <c>/automation-api</c>,
    /// with no socket, configured by <paramref name="configure"/>. The simulator seeds no key: the callback chooses the
    /// credential, such as <c>o =&gt; o.UseApiKey("APIKEY1")</c>, and runs after the endpoint is set. The client is
    /// released when the simulator is disposed.
    /// </summary>
    /// <param name="configure">Sets the client's options; it must choose an authentication scheme.</param>
    public IControlMClient CreateClient(Action<ControlMClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var server = _app.GetTestServer();
        var services = new ServiceCollection();
        services
            .AddControlMClient(o =>
            {
                o.Endpoint = new Uri(server.BaseAddress, ApiPath.Base);
                configure(o);
            })
            .ConfigurePrimaryHttpMessageHandler(server.CreateHandler);
        var provider = services.BuildServiceProvider();
        _clientProviders.Add(provider);
        return provider.GetRequiredService<IControlMClient>();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var provider in _clientProviders)
        {
            await provider.DisposeAsync();
        }

        await _app.DisposeAsync();
    }
}
