using ControlM.Client.Errors;
using ControlM.Simulator.Errors;

namespace ControlM.Simulator.Operations;

// Only PATCH /run/jobs/status was observed on a real server; requirements section 4.8 assumes the same 405 for every
// method, path and credential.
internal static class WrongMethod
{
    public static ErrorList Response(string path) => ErrorEnvelope.MethodNotAllowed(ReportedUri.Of(path));
}
