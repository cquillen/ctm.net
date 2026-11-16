using System.Net;
using ControlM.Client.Operations;

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
    /// <param name="responseBody">The text of the response body when it was not an error list.</param>
    /// <param name="method">The HTTP method of the call.</param>
    /// <param name="path">The path of the call as the caller gave it.</param>
    /// <param name="operation">The operation of the call, or <see langword="null"/> when it was not a typed call.</param>
    public ControlMApiException(
        HttpStatusCode statusCode,
        IReadOnlyList<ControlMError> errors,
        string? responseBody,
        HttpMethod method,
        string path,
        Operation? operation)
        : base($"Control-M answered {(int)statusCode}.")
    {
        ArgumentNullException.ThrowIfNull(errors);
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(path);
        StatusCode = statusCode;
        Errors = errors;
        ResponseBody = responseBody;
        Method = method;
        Path = path;
        Operation = operation;
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

    /// <summary>
    /// The HTTP method of the failed call.
    /// </summary>
    public HttpMethod Method { get; }

    /// <summary>
    /// The path of the failed call as the caller gave it, relative to the endpoint and with its query string.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// The operation of the failed call when it was made through a typed member; <see langword="null"/> when it was
    /// made through <see cref="IControlMClient.SendAsync"/>.
    /// </summary>
    public Operation? Operation { get; }
}
