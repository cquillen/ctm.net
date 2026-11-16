using ControlM.Client.Errors;

namespace ControlM.Simulator.Errors;

internal static class ErrorEnvelope
{
    public static ErrorList NotFound(string uri) => new([new ControlMError("Not Found", Code: "404", Uri: uri)]);

    public static ErrorList NoCredential() => Unauthenticated("Unauthenticated request: No authentication header was provided");

    private static ErrorList Unauthenticated(string message) => new([new ControlMError(message)]);
}
