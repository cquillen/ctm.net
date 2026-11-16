namespace ControlM.Simulator.Authentication;

internal static class CredentialCheck
{
    public const string ApiKeyHeader = "x-api-key";

    public static CredentialOutcome Check(CredentialStore store, string? apiKey, bool hasAuthorizationHeader)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            return hasAuthorizationHeader ? CredentialOutcome.Accepted : CredentialOutcome.NoCredential;
        }

        return store.HasApiKey(apiKey) ? CredentialOutcome.Accepted : CredentialOutcome.UnknownApiKey;
    }
}
