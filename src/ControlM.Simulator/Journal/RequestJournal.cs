using ControlM.Client.Operations;

namespace ControlM.Simulator.Journal;

/// <summary>
/// The requests a simulator instance received over the Control-M API, as journal entries. Calls to the control API
/// under <c>/__sim/</c> and typed controls are not recorded.
/// </summary>
public sealed class RequestJournal
{
    private readonly Lock _lock = new();

    // One slot per request, taken when it arrives and filled once it is answered, so the order is arrival order
    // even when a later request is answered first.
    private readonly List<JournalEntry?> _slots = [];

    internal RequestJournal()
    {
    }

    /// <summary>
    /// A snapshot of every entry, in the order the requests arrived. Requests that arrive later do not change it.
    /// </summary>
    public IReadOnlyList<JournalEntry> Entries
    {
        get
        {
            lock (_lock)
            {
                return [.. _slots.OfType<JournalEntry>()];
            }
        }
    }

    /// <summary>
    /// Counts every request routed to <paramref name="operation"/>, whatever it was answered, so a request refused for
    /// its credential counts too.
    /// </summary>
    /// <param name="operation">The operation, from the client's operation table.</param>
    /// <returns>The number of requests routed to the operation so far.</returns>
    public int Count(Operation operation) => Entries.Count(entry => entry.Operation == operation);

    /// <summary>
    /// The entries for requests that matched no operation, in the order they arrived: an unknown path, inside or outside
    /// <c>/automation-api</c>, answered 404, and a wrong method on a real path, answered 405. Assert it is empty to show
    /// an application called nothing that does not exist.
    /// </summary>
    public IReadOnlyList<JournalEntry> Unmatched => throw new NotImplementedException();

    internal int Admit()
    {
        lock (_lock)
        {
            _slots.Add(null);
            return _slots.Count - 1;
        }
    }

    internal void Record(int slot, JournalEntry entry)
    {
        lock (_lock)
        {
            _slots[slot] = entry;
        }
    }
}
