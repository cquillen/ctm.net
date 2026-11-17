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

        List<string> errors = [];
        var endpoint = ReadEndpoint(environment, errors);
        var credential = ReadCredential(environment, errors);
        if (endpoint is null || credential is null)
        {
            return new Invalid(errors);
        }

        return new RealServer(endpoint, credential);
    }

    private static Uri? ReadEndpoint(IReadOnlyDictionary<string, string> environment, List<string> errors)
    {
        var value = Read(environment, _endpoint);
        if (value is null)
        {
            errors.Add($"{_endpoint} is required when any {_prefix} variable is set.");
            return null;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var endpoint)
            || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
        {
            errors.Add($"{_endpoint} must be an absolute http or https URI including /automation-api.");
            return null;
        }

        return endpoint;
    }

    private static ConformanceCredential? ReadCredential(IReadOnlyDictionary<string, string> environment, List<string> errors)
    {
        var key = Read(environment, _apiKey);
        var username = Read(environment, _username);
        var password = Read(environment, _password);
        if (key is not null && (username is not null || password is not null))
        {
            errors.Add(Conflict(username, password));
            return null;
        }

        if (key is not null)
        {
            return new ConformanceCredential.ApiKey(key);
        }

        if (username is not null && password is not null)
        {
            return new ConformanceCredential.SessionLogin(username, password);
        }

        if (username is not null || password is not null)
        {
            var (missing, given) = username is null ? (_username, _password) : (_password, _username);
            errors.Add($"{missing} is required with {given}.");
            return null;
        }

        errors.Add($"A credential is required: set {_apiKey}, or {_username} with {_password}.");
        return null;
    }

    private static string Conflict(string? username, string? password)
    {
        string?[] given = [username is null ? null : _username, password is null ? null : _password];
        return $"{_apiKey} cannot be set with {string.Join(" and ", given.OfType<string>())}: set exactly one credential.";
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
