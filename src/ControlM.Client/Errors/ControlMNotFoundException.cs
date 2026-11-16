using System.Net;
using ControlM.Client.Operations;

namespace ControlM.Client.Errors;

/// <summary>
/// Thrown when Control-M answers a call with 404: the path matched no operation, or the item it names does not exist.
/// </summary>
public sealed class ControlMNotFoundException : ControlMApiException
{
    /// <summary>
    /// Creates the exception for a call answered with 404.
    /// </summary>
    /// <param name="errors">Every entry of the response's error list, or none when the body was not one.</param>
    /// <param name="responseBody">The text of the response body when it was not an error list.</param>
    /// <param name="method">The HTTP method of the call.</param>
    /// <param name="path">The path of the call as the caller gave it.</param>
    /// <param name="operation">The operation of the call, or <see langword="null"/> when it was not a typed call.</param>
    public ControlMNotFoundException(
        IReadOnlyList<ControlMError> errors,
        string? responseBody,
        HttpMethod method,
        string path,
        Operation? operation)
        : base(HttpStatusCode.NotFound, errors, responseBody, method, path, operation)
    {
    }
}
