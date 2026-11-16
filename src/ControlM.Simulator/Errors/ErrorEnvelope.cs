using ControlM.Client.Errors;

namespace ControlM.Simulator.Errors;

internal static class ErrorEnvelope
{
    public static ErrorList NotFound(string uri) => new([new ControlMError("Not Found", Code: "404", Uri: uri)]);

    public static ErrorList MethodNotAllowed(string uri) =>
        new([new ControlMError("Method Not Allowed", Code: "405", Uri: uri)]);

    public static ErrorList NoCredential() => MessageOnly("Unauthenticated request: No authentication header was provided");

    public static ErrorList BothCredentialHeaders() =>
        MessageOnly("Unauthenticated request: Request cannot contain both x-api-key and authorization headers");

    public static ErrorList InvalidOrExpiredToken() => MessageOnly("User not found. Session token is invalid or expired.");

    public static ErrorList UnknownApiKey() => MessageOnly("Cannot validate token value, decode failed");

    private static ErrorList MessageOnly(string message) => new([new ControlMError(message)]);
}
