using System.Collections.Concurrent;

namespace ControlM.Simulator.Authentication;

/// <summary>
/// The credentials one simulator instance accepts. Another instance knows none of them.
/// </summary>
public sealed class CredentialStore
{
    private readonly ConcurrentDictionary<string, bool> _apiKeys = new(StringComparer.Ordinal);

    /// <summary>
    /// Seeds an API key, so a request that sends it in <c>x-api-key</c> is accepted whatever else it carries.
    /// </summary>
    /// <param name="key">The key, as a request sends it.</param>
    public void AddApiKey(string key) => _apiKeys[key] = true;

    internal CredentialOutcome Check(string? apiKey, bool hasAuthorizationHeader)
    {
        var hasApiKey = !string.IsNullOrEmpty(apiKey);
        if (!hasApiKey && !hasAuthorizationHeader)
        {
            return CredentialOutcome.NoCredential;
        }

        if (hasApiKey && hasAuthorizationHeader)
        {
            return CredentialOutcome.BothCredentialHeaders;
        }

        // The simulator issues no bearer token, so an Authorization header alone always fails (requirements section
        // 4.8).
        if (hasAuthorizationHeader)
        {
            return CredentialOutcome.InvalidOrExpiredToken;
        }

        return _apiKeys.ContainsKey(apiKey!) ? CredentialOutcome.Accepted : CredentialOutcome.UnknownApiKey;
    }
}
