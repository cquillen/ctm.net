using Microsoft.AspNetCore.Http;

namespace ControlM.Simulator.Hosting;

internal static class ControlApiPath
{
    public static PathString Base { get; } = new("/__sim");
}
