namespace ControlM.Simulator.Hosting;

/// <summary>
/// A simulator of the Control-M Automation API, running in process with no socket. Start one per test with
/// <see cref="StartAsync"/> and dispose it when the test ends; instances share no state.
/// </summary>
public sealed class ControlMSimulator : IAsyncDisposable
{
    private ControlMSimulator()
    {
    }

    /// <summary>
    /// Starts a simulator instance.
    /// </summary>
    /// <param name="cancellationToken">Cancels the start.</param>
    /// <returns>The started instance, which serves the Control-M API under <c>/automation-api</c>.</returns>
    public static Task<ControlMSimulator> StartAsync(CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    /// <summary>
    /// Returns a raw <see cref="HttpClient"/> whose base address is the root of the simulator, so a request
    /// names the whole path, such as <c>/automation-api/run/jobs/status</c>.
    /// </summary>
    public HttpClient CreateHttpClient() => throw new NotImplementedException();

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
