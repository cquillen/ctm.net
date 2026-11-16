using System.Collections.Concurrent;

namespace ControlM.Simulator.Authentication;

/// <summary>
/// The credentials one simulator instance accepts. Another instance knows none of them.
/// </summary>
public sealed class CredentialStore
{
    private readonly ConcurrentDictionary<string, bool> _apiKeys = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _users = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, bool> _sessions = new(StringComparer.Ordinal);
    private readonly SessionTokenSequence _tokens = new();

    /// <summary>
    /// Seeds an API key, so a request that sends it in <c>x-api-key</c> is accepted, unless the request also carries an
    /// <c>Authorization</c> header.
    /// </summary>
    /// <param name="key">The key, as a request sends it.</param>
    public void AddApiKey(string key) => _apiKeys[key] = true;

    /// <summary>
    /// Seeds a user, so <c>POST /session/login</c> with this user name and password opens a session.
    /// </summary>
    /// <param name="username">The user name, as a login sends it.</param>
    /// <param name="password">The user's password.</param>
    public void AddUser(string username, string password) => _users[username] = password;

    internal LoginOutcome Login(string? username, string? password)
    {
        if (username is null || password is null || !_users.TryGetValue(username, out var seededPassword))
        {
            return LoginOutcome.Failed(CredentialFailure.IncorrectUsernameOrPassword());
        }

        if (!string.Equals(password, seededPassword, StringComparison.Ordinal))
        {
            return LoginOutcome.Failed(CredentialFailure.WrongPassword());
        }

        var token = _tokens.Next();
        _sessions[token] = true;
        return LoginOutcome.Succeeded(token);
    }

    internal CredentialFailure? Check(string? apiKey, bool hasAuthorizationHeader)
    {
        var hasApiKey = !string.IsNullOrEmpty(apiKey);
        if (!hasApiKey && !hasAuthorizationHeader)
        {
            return CredentialFailure.NoCredential();
        }

        if (hasApiKey && hasAuthorizationHeader)
        {
            return CredentialFailure.BothCredentialHeaders();
        }

        // The simulator issues no bearer token, so an Authorization header alone always fails (requirements section
        // 4.8).
        if (hasAuthorizationHeader)
        {
            return CredentialFailure.InvalidOrExpiredToken();
        }

        return _apiKeys.ContainsKey(apiKey!) ? null : CredentialFailure.UnknownApiKey();
    }
}
