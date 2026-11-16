using ControlM.Client.Authentication;

namespace ControlM.Simulator.Authentication.Logout;

internal static class LogoutResponse
{
    public static SuccessData Success(string session) => new($"Successfully logged out from session {session}");
}
