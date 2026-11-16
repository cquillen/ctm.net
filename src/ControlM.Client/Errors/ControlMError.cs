namespace ControlM.Client.Errors;

// Message, Id, Item, File, Line and Col are the fields of ErrorData in BMC's Python client
// (controlm/ctm-python-client). Code and Uri were observed on a real server: see docs/adr/0001-unknown-path-answers-404.md.

/// <summary>
/// One entry of the error list Control-M sends with a failure.
/// </summary>
/// <param name="Message">The server's description of the error.</param>
/// <param name="Id">The server's identifier for the kind of error, when it sends one.</param>
/// <param name="Item">The item the error concerns, such as a job or a folder, when the server names one.</param>
/// <param name="File">The file the error was found in, when the server names one.</param>
/// <param name="Line">The line of <paramref name="File"/> the error is on, when the server gives one.</param>
/// <param name="Col">The column of <paramref name="Line"/> the error is at, when the server gives one.</param>
/// <param name="Code">The server's error code, such as <c>404</c>. The server sends it as a string.</param>
/// <param name="Uri">The path the server reports for the request, without the <c>/automation-api</c> prefix or the query string.</param>
public sealed record ControlMError(
    string Message,
    string? Id = null,
    string? Item = null,
    string? File = null,
    int? Line = null,
    int? Col = null,
    string? Code = null,
    string? Uri = null);
