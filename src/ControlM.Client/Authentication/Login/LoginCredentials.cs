namespace ControlM.Client.Authentication.Login;

// The fields of LoginCredentials in BMC's Python client (controlm/ctm-python-client): the body of POST /session/login.
internal sealed record LoginCredentials(string Username, string Password);
