namespace ControlM.Client.Authentication.SessionLogin;

internal sealed class SessionLoginScheme : ICredentialScheme
{
    public ValueTask ApplyAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
