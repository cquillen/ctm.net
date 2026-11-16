namespace ControlM.Client.Operations;

// The operation table: one property per implemented operation (docs/requirements.md section 2). The rows sit
// together because a partial type cannot follow the feature folders' namespaces.
public sealed partial record Operation
{
    /// <summary>
    /// <c>GET /config/servers</c>: the servers the Automation API knows.
    /// </summary>
    public static Operation GetServers { get; } = new(nameof(GetServers), HttpMethod.Get, "/config/servers");
}
