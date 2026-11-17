using ControlM.Client;

namespace ControlM.Conformance.Tests;

internal sealed class ConformanceTarget : IAsyncDisposable
{
    public IControlMClient Client => throw new NotImplementedException();

    public static Task<ConformanceTarget> StartAsync() => throw new NotImplementedException();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
