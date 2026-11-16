namespace ControlM.Simulator.Operations;

// The uri a real server reports in the error entry of a request that matched no operation: the path without the
// /automation-api prefix or the query string (ADR 0001).
internal static class ReportedUri
{
    public static string Of(string path) =>
        path.StartsWith(ApiPath.Base + "/", StringComparison.Ordinal)
            ? path[ApiPath.Base.Length..]
            : UriOutsideApi(path);

    // Only paths under /automation-api were observed on a real server; requirements section 4.8 assumes
    // a path outside it reports its whole path.
    private static string UriOutsideApi(string path) => path;
}
