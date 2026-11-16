using ControlM.Client.Authentication;
using ControlM.Client.Authentication.ApiKey;

namespace ControlM.Client;

/// <summary>
/// The settings of a registered <see cref="IControlMClient"/>. They are checked when the client is first resolved.
/// </summary>
public sealed class ControlMClientOptions
{
    /// <summary>
    /// The address of the Automation API, including its <c>/automation-api</c> prefix, such as
    /// <c>https://host1:8443/automation-api</c>. Required. A trailing slash makes no difference.
    /// </summary>
    public Uri? Endpoint { get; set; }

    internal ICredentialScheme? CredentialScheme { get; private set; }

    /// <summary>
    /// Authenticates every request with an API key, which BMC calls an API token, sent in the <c>x-api-key</c>
    /// header. A key that is <see langword="null"/>, empty or whitespace fails when the client is first resolved.
    /// </summary>
    /// <param name="key">The API key.</param>
    public void UseApiKey(string key) => CredentialScheme = new ApiKeyScheme(key);

    /// <summary>
    /// Authenticates with session login: the client logs in with the user name and password on its first request, and
    /// sends the token it receives as <c>Authorization: Bearer</c>. A user name or password that is
    /// <see langword="null"/>, empty or whitespace fails when the client is first resolved, as does choosing
    /// <see cref="UseApiKey"/> as well.
    /// </summary>
    /// <param name="username">The Control-M user name.</param>
    /// <param name="password">The user's password.</param>
    public void UseSessionLogin(string username, string password) => throw new NotImplementedException();
}
