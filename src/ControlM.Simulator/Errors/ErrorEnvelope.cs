using ControlM.Client.Errors;

namespace ControlM.Simulator.Errors;

internal static class ErrorEnvelope
{
    public static ErrorList NotFound(string uri) => new([new ControlMError("Not Found", Code: "404", Uri: uri)]);

    public static ErrorList NoCredential() => Unauthenticated("Unauthenticated request: No authentication header was provided");

    public static ErrorList InvalidOrExpiredToken() => Unauthenticated("User not found. Session token is invalid or expired.");

    public static ErrorList UnknownApiKey() => Unauthenticated(UnknownApiKeyMessage);

    // Assumed, requirements section 4.8: no API token was available to probe a real server with.
    private static string UnknownApiKeyMessage => "Invalid API token.";

    private static ErrorList Unauthenticated(string message) => new([new ControlMError(message)]);
}
