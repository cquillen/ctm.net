namespace ControlM.Simulator.Authentication;

internal sealed record LogoutOutcome(string? Credential, CredentialFailure? Failure)
{
    public static LogoutOutcome Succeeded(string credential) => new(credential, null);

    public static LogoutOutcome Failed(CredentialFailure failure) => new(null, failure);
}
