namespace ControlM.Conformance.Tests;

internal abstract record ConformanceSelection
{
    private const string _prefix = "CTM_CONFORMANCE_";

    public static ConformanceSelection From(IReadOnlyDictionary<string, string> environment)
    {
        if (!environment.Keys.Any(name => name.StartsWith(_prefix, StringComparison.Ordinal)))
        {
            return new Simulator();
        }

        return new Invalid([$"{_prefix}ENDPOINT is required when any {_prefix} variable is set."]);
    }

    public sealed record Simulator : ConformanceSelection;

    public sealed record RealServer(Uri Endpoint, ConformanceCredential Credential) : ConformanceSelection;

    public sealed record Invalid(IReadOnlyList<string> Errors) : ConformanceSelection;
}
