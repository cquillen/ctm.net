namespace ControlM.Client;

/// <summary>
/// The settings of a registered <see cref="IControlMClient"/>. They are checked when the client is first resolved.
/// </summary>
public sealed record ControlMClientOptions
{
    /// <summary>
    /// The address of the Automation API, including its <c>/automation-api</c> prefix, such as
    /// <c>https://host1:8443/automation-api</c>. Required. A trailing slash makes no difference.
    /// </summary>
    public Uri? Endpoint { get; init; }
}
