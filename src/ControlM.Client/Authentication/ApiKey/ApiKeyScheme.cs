namespace ControlM.Client.Authentication.ApiKey;

internal sealed class ApiKeyScheme(string? key) : ICredentialScheme
{
    public const string HeaderName = "x-api-key";

    public void Apply(HttpRequestMessage request) => request.Headers.Add(HeaderName, key);
}
