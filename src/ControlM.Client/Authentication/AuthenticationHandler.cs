namespace ControlM.Client.Authentication;

internal sealed class AuthenticationHandler(ICredentialScheme scheme) : DelegatingHandler
{
    public static HttpRequestOptionsKey<bool> Unauthenticated { get; } = new("ControlM.Unauthenticated");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Options.TryGetValue(Unauthenticated, out var unauthenticated) && unauthenticated)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        await scheme.ApplyAsync(request, cancellationToken);
        return await base.SendAsync(request, cancellationToken);
    }
}
