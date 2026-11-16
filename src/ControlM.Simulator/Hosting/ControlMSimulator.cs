using ControlM.Client;
using ControlM.Simulator.Operations.UnknownOperation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace ControlM.Simulator.Hosting;

/// <summary>
/// A simulator of the Control-M Automation API, running in process with no socket. Start one per test with
/// <see cref="StartAsync"/> and dispose it when the test ends; instances share no state.
/// </summary>
public sealed class ControlMSimulator : IAsyncDisposable
{
    private readonly WebApplication _app;

    private ControlMSimulator(WebApplication app) => _app = app;

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
        UnknownOperationEndpoint.Map(app);
        await app.StartAsync(cancellationToken);
        return new ControlMSimulator(app);
    }

    /// <summary>
    /// Returns a raw <see cref="HttpClient"/> whose base address is the root of the simulator, so a request
    /// names the whole path, such as <c>/automation-api/run/jobs/status</c>.
    /// </summary>
    public HttpClient CreateHttpClient() => _app.GetTestServer().CreateClient();

    /// <summary>
    /// Returns a real <see cref="IControlMClient"/> whose requests reach this instance under <c>/automation-api</c>,
    /// with no socket. The client is released when the simulator is disposed.
    /// </summary>
    public IControlMClient CreateClient() => throw new NotImplementedException();

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _app.DisposeAsync();
}
