namespace ControlM.Conformance.Tests;

internal abstract record ConformanceSelection
{
    private const string _prefix = "CTM_CONFORMANCE_";
    private const string _endpoint = _prefix + "ENDPOINT";
    private const string _apiKey = _prefix + "API_KEY";
    private const string _username = _prefix + "USERNAME";
    private const string _password = _prefix + "PASSWORD";

    public static ConformanceSelection From(IReadOnlyDictionary<string, string> environment)
    {
        if (!environment.Keys.Any(name => name.StartsWith(_prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return new Simulator();
        }

        var endpoint = Read(environment, _endpoint);
        var key = Read(environment, _apiKey);
        if (endpoint is not null && key is not null)
        {
            return new RealServer(new Uri(endpoint), new ConformanceCredential.ApiKey(key));
        }

        List<string> errors = [];
        if (endpoint is null)
        {
            errors.Add($"{_endpoint} is required when any {_prefix} variable is set.");
        }

        if (key is null)
        {
            errors.Add($"A credential is required: set {_apiKey}, or {_username} with {_password}.");
        }

        return new Invalid(errors);
    }

    private static string? Read(IReadOnlyDictionary<string, string> environment, string name)
    {
        var value = environment
            .FirstOrDefault(variable => string.Equals(variable.Key, name, StringComparison.OrdinalIgnoreCase))
            .Value;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public sealed record Simulator : ConformanceSelection;

    public sealed record RealServer(Uri Endpoint, ConformanceCredential Credential) : ConformanceSelection;

    public sealed record Invalid(IReadOnlyList<string> Errors) : ConformanceSelection;
}
