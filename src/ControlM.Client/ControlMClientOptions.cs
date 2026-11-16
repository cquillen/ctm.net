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
}
