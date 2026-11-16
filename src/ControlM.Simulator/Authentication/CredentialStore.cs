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

    internal bool HasApiKey(string key) => _apiKeys.ContainsKey(key);
}
