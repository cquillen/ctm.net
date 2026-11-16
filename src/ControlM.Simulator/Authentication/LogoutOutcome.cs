namespace ControlM.Simulator.Authentication;

internal sealed record LogoutOutcome(string? Session, CredentialFailure? Failure)
{
    public static LogoutOutcome Succeeded(string session) => new(session, null);

    public static LogoutOutcome Failed(CredentialFailure failure) => new(null, failure);
}
