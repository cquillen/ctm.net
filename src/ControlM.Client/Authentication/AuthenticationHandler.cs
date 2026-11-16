using System.Net;

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

        if (request.Content is { } content)
        {
            await content.LoadIntoBufferAsync(cancellationToken);
        }

        await scheme.ApplyAsync(request, cancellationToken);
        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized || !await TryRecoverAsync(request, response, cancellationToken))
        {
            return response;
        }

        response.Dispose();
        await scheme.ApplyAsync(request, cancellationToken);
        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<bool> TryRecoverAsync(HttpRequestMessage request, HttpResponseMessage refusal, CancellationToken cancellationToken)
    {
        try
        {
            return await scheme.TryRecoverAsync(request, cancellationToken);
        }
        catch
        {
            refusal.Dispose();
            throw;
        }
    }
}
