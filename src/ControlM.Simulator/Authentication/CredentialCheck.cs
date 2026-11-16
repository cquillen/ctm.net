namespace ControlM.Simulator.Authentication;

internal static class CredentialCheck
{
    public const string ApiKeyHeader = "x-api-key";

    public static CredentialOutcome Check(string? apiKey, bool hasAuthorizationHeader) =>
        string.IsNullOrEmpty(apiKey) && !hasAuthorizationHeader
            ? CredentialOutcome.NoCredential
            : CredentialOutcome.Accepted;
}
