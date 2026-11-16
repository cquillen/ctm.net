using System.Collections.Concurrent;
using System.Diagnostics.Tracing;

namespace ControlM.Simulator.Tests.Hosting;

// Observes service providers through the events Microsoft.Extensions.DependencyInjection publishes, so a test can
// see a provider built and disposed without reaching into the simulator. No caller-visible effect shows the
// disposal: a client used afterwards fails on the disposed test server whether or not its provider was disposed.
// The events identify a provider only by its hash code, never by reference. So that another test running in
// parallel cannot add or match an event, both events are recorded only within the async flow that RecordAsync runs.
// Disposing a provider disposes its root scope, which raises ScopeDisposed; there is no provider-disposed event.
internal sealed class ServiceProviderEvents : EventListener
{
    private readonly AsyncLocal<Recording?> _recording = new();

    public async Task<Recording> RecordAsync(Func<Task> action)
    {
        var recording = new Recording();
        _recording.Value = recording;
        await action();
        return recording;
    }

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name == "Microsoft-Extensions-DependencyInjection")
        {
            EnableEvents(eventSource, EventLevel.Verbose);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        if (_recording.Value is not { } recording || eventData.Payload is not [int providerHashCode, ..])
        {
            return;
        }

        switch (eventData.EventName)
        {
            case "ServiceProviderBuilt":
                recording.Built.Enqueue(providerHashCode);
                return;
            case "ScopeDisposed":
                recording.Disposed.Enqueue(providerHashCode);
                return;
        }
    }

    internal sealed class Recording
    {
        public ConcurrentQueue<int> Built { get; } = new();

        public ConcurrentQueue<int> Disposed { get; } = new();
    }
}
