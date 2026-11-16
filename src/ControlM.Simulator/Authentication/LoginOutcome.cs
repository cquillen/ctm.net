namespace ControlM.Simulator.Authentication;

internal sealed record LoginOutcome(string? Token, CredentialFailure? Failure)
{
    public static LoginOutcome Succeeded(string token) => new(token, null);

    public static LoginOutcome Failed(CredentialFailure failure) => new(null, failure);
}
