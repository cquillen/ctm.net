using System.Net;

namespace ControlM.Client.Errors;

/// <summary>
/// Thrown when Control-M answers a call with a failure status.
/// </summary>
public class ControlMApiException : Exception
{
    /// <summary>
    /// Creates the exception for a failed call.
    /// </summary>
    /// <param name="statusCode">The status code of the response.</param>
    /// <param name="errors">Every entry of the response's error list, or none when the body was not one.</param>
    public ControlMApiException(HttpStatusCode statusCode, IReadOnlyList<ControlMError> errors)
        : base($"Control-M answered {(int)statusCode}.")
    {
        ArgumentNullException.ThrowIfNull(errors);
        StatusCode = statusCode;
        Errors = errors;
    }

    /// <summary>
    /// The status code of the response.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Every entry of the response's error list, in the server's order. Empty when the body was not an error list.
    /// </summary>
    public IReadOnlyList<ControlMError> Errors { get; }

    /// <summary>
    /// The text of the response body when it was not an error list, such as a proxy's HTML page; otherwise
    /// <see langword="null"/>.
    /// </summary>
    public string? ResponseBody { get; }
}
