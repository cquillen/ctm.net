using ControlM.Client.Operations;

namespace ControlM.Simulator.Journal;

/// <summary>
/// The requests a simulator instance received over the Control-M API, as journal entries. Calls to the control API
/// under <c>/__sim/</c> and typed controls are not recorded.
/// </summary>
public sealed class RequestJournal
{
    internal RequestJournal()
    {
    }

    /// <summary>
    /// Counts every request routed to <paramref name="operation"/>, whatever it was answered, so a request refused for
    /// its credential counts too.
    /// </summary>
    /// <param name="operation">The operation, from the client's operation table.</param>
    /// <returns>The number of requests routed to the operation so far.</returns>
    public int Count(Operation operation) => throw new NotImplementedException();
}
