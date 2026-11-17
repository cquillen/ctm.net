namespace ControlM.Simulator.Tests.Journal;

internal static class RequestUris
{
    public static Uri GetServers { get; } = new("/automation-api/config/servers", UriKind.Relative);
}
