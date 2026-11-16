using ControlM.Client.Errors;
using ControlM.Simulator.Errors;

namespace ControlM.Simulator.Operations;

internal static class UnknownPath
{
    public static ErrorList Response(string path) => ErrorEnvelope.NotFound(ReportedUri(path));

    private static string ReportedUri(string path) =>
        path.StartsWith(ApiPath.Base + "/", StringComparison.Ordinal)
            ? path[ApiPath.Base.Length..]
            : UriOutsideApi(path);

    // Only paths under /automation-api were observed on a real server; requirements section 4.8 assumes
    // a path outside it reports its whole path.
    private static string UriOutsideApi(string path) => path;
}
