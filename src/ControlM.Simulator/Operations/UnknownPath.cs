using ControlM.Client.Errors;
using ControlM.Simulator.Errors;

namespace ControlM.Simulator.Operations;

internal static class UnknownPath
{
    public static ErrorList Response(string path) => ErrorEnvelope.NotFound(ReportedUri.Of(path));
}
