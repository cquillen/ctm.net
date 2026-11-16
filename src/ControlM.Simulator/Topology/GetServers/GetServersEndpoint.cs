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

    // There is no topology model yet, so every simulator has no servers.
    private static IResult Answer() => Results.Json(Array.Empty<object>(), SimulatorJson.Options);
}
