using ControlM.Client.Operations;

namespace ControlM.Simulator.Journal;

/// <summary>
/// One request the simulator received, with how it was answered.
/// </summary>
/// <param name="Operation">The operation the request was routed to, or <see langword="null"/> for an unknown operation.</param>
public sealed record JournalEntry(Operation? Operation);
