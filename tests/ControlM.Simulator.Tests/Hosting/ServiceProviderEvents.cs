using System.Collections.Concurrent;
using System.Diagnostics.Tracing;

namespace ControlM.Simulator.Tests.Hosting;

// Observes service providers through the events Microsoft.Extensions.DependencyInjection publishes, so a test can
// see a provider built and disposed without reaching into the simulator. The listener is process wide and tests
// run in parallel, so built providers are collected only within the caller's async flow, and every event is keyed
// by the provider's hash code. Disposing a provider disposes its root scope, which raises ScopeDisposed with the
// provider's hash code; there is no separate provider-disposed event.
internal sealed class ServiceProviderEvents : EventListener
{
    private readonly AsyncLocal<List<int>?> _collector = new();
    private readonly ConcurrentDictionary<int, bool> _disposed = new();

    public IReadOnlyList<int> ProvidersBuiltBy(Action action)
    {
        var built = new List<int>();
        _collector.Value = built;
        action();
        _collector.Value = null;
        return built;
    }

    public bool SawDisposed(int providerHashCode) => _disposed.ContainsKey(providerHashCode);

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name == "Microsoft-Extensions-DependencyInjection")
        {
            EnableEvents(eventSource, EventLevel.Verbose);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        if (eventData.Payload is not [int providerHashCode, ..])
        {
            return;
        }

        switch (eventData.EventName)
        {
            case "ServiceProviderBuilt":
                _collector.Value?.Add(providerHashCode);
                return;
            case "ScopeDisposed":
                _disposed[providerHashCode] = true;
                return;
        }
    }
}
