namespace ControlM.Conformance.Tests;

internal abstract record ConformanceSelection
{
    private const string Prefix = "CTM_CONFORMANCE_";

    public static ConformanceSelection From(IReadOnlyDictionary<string, string> environment)
    {
        if (!environment.Keys.Any(name => name.StartsWith(Prefix, StringComparison.Ordinal)))
        {
            return new Simulator();
        }

        return new Invalid([$"{Prefix}ENDPOINT is required when any {Prefix} variable is set."]);
    }

    public sealed record Simulator : ConformanceSelection;

    public sealed record Invalid(IReadOnlyList<string> Errors) : ConformanceSelection;
}
