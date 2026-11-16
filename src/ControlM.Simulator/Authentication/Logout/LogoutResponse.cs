using ControlM.Client.Authentication;

namespace ControlM.Simulator.Authentication.Logout;

internal static class LogoutResponse
{
    public static SuccessData Success(string credential) => new($"Successfully logged out from session {credential}");
}
