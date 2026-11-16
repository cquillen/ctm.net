using System.Collections.Concurrent;
using System.Net.Http.Headers;
using ControlM.Client.Authentication.SessionLogin;

namespace ControlM.Simulator.Authentication;

/// <summary>
/// The credentials one simulator instance accepts. Another instance knows none of them.
/// </summary>
public sealed class CredentialStore
{
    private readonly ConcurrentDictionary<string, bool> _apiKeys = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _users = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Session> _sessions = new(StringComparer.Ordinal);
    private readonly SessionTokenSequence _tokens = new();
    private readonly TimeProvider _time;
    private readonly TimeSpan _sessionLifetime;

    internal CredentialStore(TimeProvider time, TimeSpan sessionLifetime)
    {
        _time = time;
        _sessionLifetime = sessionLifetime;
    }

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

    /// <summary>
    /// Ends every live session, as if each had gone unused for its whole lifetime. Each token is refused from then on,
    /// and a session client logs in again on its next call.
    /// </summary>
    public void ExpireSessions() => _sessions.Clear();

    /// <summary>
    /// Ends the session of one token and leaves the others live. A token that is not live is ignored.
    /// </summary>
    /// <param name="token">The token login returned, such as <c>SIMTOKEN1</c>.</param>
    public void ExpireSession(string token) => throw new NotImplementedException();

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
        _sessions[token] = new Session(_time.GetUtcNow());
        return LoginOutcome.Succeeded(token);
    }

    internal CredentialFailure? Check(string? apiKey, string? authorization)
    {
        var hasApiKey = !string.IsNullOrEmpty(apiKey);
        var hasAuthorization = authorization is not null;
        if (!hasApiKey && !hasAuthorization)
        {
            return CredentialFailure.NoCredential();
        }

        if (hasApiKey && hasAuthorization)
        {
            return CredentialFailure.BothCredentialHeaders();
        }

        if (hasAuthorization)
        {
            return TryUseSession(authorization!) ? null : CredentialFailure.InvalidOrExpiredToken();
        }

        return _apiKeys.ContainsKey(apiKey!) ? null : CredentialFailure.UnknownApiKey();
    }

    private bool TryUseSession(string authorization) =>
        AuthenticationHeaderValue.TryParse(authorization, out var header)
        && string.Equals(header.Scheme, SessionLoginScheme.AuthenticationScheme, StringComparison.OrdinalIgnoreCase)
        && header.Parameter is { } token
        && _sessions.TryGetValue(token, out var session)
        && session.TryUse(_time.GetUtcNow(), _sessionLifetime);
}
