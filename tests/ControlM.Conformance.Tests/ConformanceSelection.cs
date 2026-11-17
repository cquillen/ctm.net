namespace ControlM.Conformance.Tests;

internal abstract record ConformanceSelection
{
    public static ConformanceSelection From(IReadOnlyDictionary<string, string> environment) =>
        throw new NotImplementedException();

    public sealed record Simulator : ConformanceSelection;
}
