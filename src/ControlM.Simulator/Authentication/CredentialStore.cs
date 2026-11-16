namespace ControlM.Simulator.Authentication;

/// <summary>
/// The credentials one simulator instance accepts. Another instance knows none of them.
/// </summary>
public sealed class CredentialStore
{
    /// <summary>
    /// Seeds an API key, so a request that sends it in <c>x-api-key</c> is accepted whatever else it carries.
    /// </summary>
    /// <param name="key">The key, as a request sends it.</param>
    public void AddApiKey(string key) => throw new NotImplementedException();
}
