namespace ControlM.Conformance.Tests;

internal abstract record ConformanceSelection
{
    public static ConformanceSelection From(IReadOnlyDictionary<string, string> environment)
    {
        if (environment.Count == 0)
        {
            return new Simulator();
        }

        return new Invalid(["CTM_CONFORMANCE_ENDPOINT is required when any CTM_CONFORMANCE_ variable is set."]);
    }

    public sealed record Simulator : ConformanceSelection;

    public sealed record Invalid(IReadOnlyList<string> Errors) : ConformanceSelection;
}
