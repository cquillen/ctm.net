namespace ControlM.Client.Authentication.Login;

// The fields of LoginResult in BMC's Python client (controlm/ctm-python-client): the body of a successful
// POST /session/login.
internal sealed record LoginResult(string Username, string Token, string Version);
