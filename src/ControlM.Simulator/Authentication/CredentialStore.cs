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

    internal LogoutOutcome Logout(string? apiKey, string? authorization)
    {
        if (HeaderPresenceFailure(apiKey, authorization) is { } failure)
        {
            return LogoutOutcome.Failed(failure);
        }

        if (authorization is null)
        {
            return LogOutApiKey(apiKey!);
        }

        if (BearerToken(authorization) is not { } token)
        {
            return LogOutOtherAuthorization(authorization);
        }

        _sessions.TryRemove(token, out _);
        return LogoutOutcome.Succeeded(token);
    }

    // Assumed (requirements section 4.8): no real server has been sent a logout with an API key.
    private static LogoutOutcome LogOutApiKey(string apiKey) => LogoutOutcome.Succeeded(apiKey);

    // Assumed (requirements section 4.8): no real server has been sent a logout whose Authorization is not a bearer token.
    private static LogoutOutcome LogOutOtherAuthorization(string authorization) => LogoutOutcome.Succeeded(authorization);

    internal CredentialFailure? Check(string? apiKey, string? authorization)
    {
        if (HeaderPresenceFailure(apiKey, authorization) is { } failure)
        {
            return failure;
        }

        if (authorization is not null)
        {
            return IsLiveSession(authorization) ? null : CredentialFailure.InvalidOrExpiredToken();
        }

        return _apiKeys.ContainsKey(apiKey!) ? null : CredentialFailure.UnknownApiKey();
    }

    // The first step of every credential check, and the only one logout applies: which headers are present, before
    // any value is read.
    private static CredentialFailure? HeaderPresenceFailure(string? apiKey, string? authorization)
    {
        var hasApiKey = !string.IsNullOrEmpty(apiKey);
        var hasAuthorization = authorization is not null;
        if (!hasApiKey && !hasAuthorization)
        {
            return CredentialFailure.NoCredential();
        }

        return hasApiKey && hasAuthorization ? CredentialFailure.BothCredentialHeaders() : null;
    }

    private bool IsLiveSession(string authorization) =>
        BearerToken(authorization) is { } token && _sessions.ContainsKey(token);

    private static string? BearerToken(string authorization) =>
        AuthenticationHeaderValue.TryParse(authorization, out var header)
        && string.Equals(header.Scheme, SessionLoginScheme.AuthenticationScheme, StringComparison.OrdinalIgnoreCase)
            ? header.Parameter
            : null;
}
