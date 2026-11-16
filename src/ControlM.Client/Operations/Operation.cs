namespace ControlM.Client.Operations;

/// <summary>
/// One operation of the Automation API, as the client names it.
/// </summary>
/// <param name="Name">The name of the operation.</param>
/// <param name="Method">The HTTP method of the operation.</param>
/// <param name="Path">The path template of the operation relative to the endpoint, such as <c>/run/job/{jobId}/status</c>.</param>
public sealed partial record Operation(string Name, HttpMethod Method, string Path);
