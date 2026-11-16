namespace ControlM.Client.Authentication;

internal interface ICredentialScheme
{
    ValueTask ApplyAsync(HttpRequestMessage request, CancellationToken cancellationToken);
}
