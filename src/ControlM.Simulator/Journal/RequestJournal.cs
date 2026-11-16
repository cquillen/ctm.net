using System.Collections.Concurrent;
using ControlM.Client.Operations;

namespace ControlM.Simulator.Journal;

/// <summary>
/// The requests a simulator instance received over the Control-M API, as journal entries. Calls to the control API
/// under <c>/__sim/</c> and typed controls are not recorded.
/// </summary>
public sealed class RequestJournal
{
    private readonly ConcurrentBag<JournalEntry> _entries = [];

    internal RequestJournal()
    {
    }

    /// <summary>
    /// A snapshot of every entry, in the order the requests arrived. Requests that arrive later do not change it.
    /// </summary>
    public IReadOnlyList<JournalEntry> Entries => [.. _entries];

    /// <summary>
    /// Counts every request routed to <paramref name="operation"/>, whatever it was answered, so a request refused for
    /// its credential counts too.
    /// </summary>
    /// <param name="operation">The operation, from the client's operation table.</param>
    /// <returns>The number of requests routed to the operation so far.</returns>
    public int Count(Operation operation) => _entries.Count(entry => entry.Operation == operation);

    internal void Record(JournalEntry entry) => _entries.Add(entry);
}
