using ControlM.Client.Authentication.Login;

namespace ControlM.Simulator.Authentication.Login;

internal static class LoginResponse
{
    // The API version of the server this project targets (requirements section 7).
    private const string Version = "9.22.60";

    public static LoginResult Success(string username, string token) => new(username, token, Version);
}
