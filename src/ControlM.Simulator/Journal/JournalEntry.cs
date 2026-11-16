using ControlM.Client.Operations;

namespace ControlM.Simulator.Journal;

/// <summary>
/// One request the simulator received, with how it was answered.
/// </summary>
/// <param name="Operation">The operation the request was routed to, or <see langword="null"/> for an unknown operation.</param>
/// <param name="Method">The HTTP method, such as <c>GET</c>.</param>
/// <param name="Path">The whole path as received, <c>/automation-api</c> included, without the query string.</param>
/// <param name="Query">The query string after the <c>?</c>, without it; empty when there is none.</param>
/// <param name="Status">The status code the request was answered with, or <see langword="null"/> when no response was sent.</param>
public sealed record JournalEntry(Operation? Operation, string Method, string Path, string Query, int? Status);
