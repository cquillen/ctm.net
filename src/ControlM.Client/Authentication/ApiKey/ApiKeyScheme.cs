namespace ControlM.Client.Authentication.ApiKey;

internal sealed class ApiKeyScheme(string? key) : ICredentialScheme
{
    public const string HeaderName = "x-api-key";

    public bool IsBlank => string.IsNullOrWhiteSpace(key);

    public void Apply(HttpRequestMessage request) => request.Headers.Add(HeaderName, key);
}
