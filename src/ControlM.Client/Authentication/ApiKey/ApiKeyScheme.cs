namespace ControlM.Client.Authentication.ApiKey;

internal sealed class ApiKeyScheme(string? key) : ICredentialScheme
{
    public const string HeaderName = "x-api-key";

    public bool IsBlank => string.IsNullOrWhiteSpace(key);

    public ValueTask ApplyAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Remove(HeaderName);
        request.Headers.Add(HeaderName, key);
        return ValueTask.CompletedTask;
    }
}
