namespace ControlM.Simulator.Authentication;

internal static class CredentialCheck
{
    // No bearer token is valid until session login exists, so an Authorization header without a seeded key fails.
    public static CredentialOutcome Check(CredentialStore store, string? apiKey, bool hasAuthorizationHeader)
    {
        if (!string.IsNullOrEmpty(apiKey) && store.HasApiKey(apiKey))
        {
            return CredentialOutcome.Accepted;
        }

        if (hasAuthorizationHeader)
        {
            return CredentialOutcome.InvalidOrExpiredToken;
        }

        return string.IsNullOrEmpty(apiKey) ? CredentialOutcome.NoCredential : CredentialOutcome.UnknownApiKey;
    }
}
