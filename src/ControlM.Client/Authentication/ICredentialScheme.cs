namespace ControlM.Client.Authentication;

internal interface ICredentialScheme
{
    void Apply(HttpRequestMessage request);
}
