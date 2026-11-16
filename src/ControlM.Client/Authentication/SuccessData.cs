namespace ControlM.Client.Authentication;

// The fields of SuccessData in BMC's Python client (controlm/ctm-python-client): the body of an operation that
// answers with a message only, such as POST /session/logout.
internal sealed record SuccessData(string Message);
