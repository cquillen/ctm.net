namespace ControlM.Client.Authentication;

internal sealed class AuthenticationHandler(ICredentialScheme scheme) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await scheme.ApplyAsync(request, cancellationToken);
        return await base.SendAsync(request, cancellationToken);
    }
}
