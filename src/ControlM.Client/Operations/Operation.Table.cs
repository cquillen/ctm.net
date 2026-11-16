namespace ControlM.Client.Operations;

// The operation table: one property per implemented operation (docs/requirements.md section 2). The rows sit
// together because a partial type cannot follow the feature folders' namespaces.
public sealed partial record Operation
{
    /// <summary>
    /// <c>GET /config/servers</c>: the servers the Automation API knows.
    /// </summary>
    public static Operation GetServers { get; } = new(nameof(GetServers), HttpMethod.Get, "/config/servers");

    /// <summary>
    /// <c>POST /session/login</c>: opens a session and returns its token. A client configured for session login sends
    /// it itself.
    /// </summary>
    public static Operation Login => throw new NotImplementedException();

    /// <summary>
    /// <c>POST /session/logout</c>: ends the session of the token the request carries.
    /// </summary>
    public static Operation Logout => throw new NotImplementedException();
}
