namespace ControlM.Client.Authentication;

internal sealed class AuthenticationHandler(ICredentialScheme scheme) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        scheme.Apply(request);
        return base.SendAsync(request, cancellationToken);
    }
}
