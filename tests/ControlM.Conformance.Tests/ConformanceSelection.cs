namespace ControlM.Conformance.Tests;

internal abstract record ConformanceSelection
{
    public static ConformanceSelection From(IReadOnlyDictionary<string, string> environment) => new Simulator();

    public sealed record Simulator : ConformanceSelection;

    public sealed record Invalid(IReadOnlyList<string> Errors) : ConformanceSelection;
}
