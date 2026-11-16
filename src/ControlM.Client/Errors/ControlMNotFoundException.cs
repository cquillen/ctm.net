using System.Net;

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
    public ControlMNotFoundException(IReadOnlyList<ControlMError> errors)
        : base(HttpStatusCode.NotFound, errors)
    {
    }
}
