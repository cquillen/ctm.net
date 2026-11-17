namespace ControlM.Conformance.Tests;

internal abstract record ConformanceSelection
{
    private const string _prefix = "CTM_CONFORMANCE_";
    private const string _endpoint = _prefix + "ENDPOINT";
    private const string _apiKey = _prefix + "API_KEY";

    public static ConformanceSelection From(IReadOnlyDictionary<string, string> environment)
    {
        if (!environment.Keys.Any(name => name.StartsWith(_prefix, StringComparison.Ordinal)))
        {
            return new Simulator();
        }

        if (environment.TryGetValue(_endpoint, out var endpoint) && environment.TryGetValue(_apiKey, out var key))
        {
            return new RealServer(new Uri(endpoint), new ConformanceCredential.ApiKey(key));
        }

        return new Invalid([$"{_endpoint} is required when any {_prefix} variable is set."]);
    }

    public sealed record Simulator : ConformanceSelection;

    public sealed record RealServer(Uri Endpoint, ConformanceCredential Credential) : ConformanceSelection;

    public sealed record Invalid(IReadOnlyList<string> Errors) : ConformanceSelection;
}
