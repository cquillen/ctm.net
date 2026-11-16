using ControlM.Client.Operations;
using ControlM.Simulator.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ControlM.Simulator.Topology.GetServers;

internal static class GetServersEndpoint
{
    private static Operation Operation => Operation.GetServers;

    public static void Map(IEndpointRouteBuilder routes) =>
        routes.MapMethods(Operation.Path, [Operation.Method.Method], Answer);

    private static IResult Answer() => Results.Json(Array.Empty<object>(), SimulatorJson.Options);
}
