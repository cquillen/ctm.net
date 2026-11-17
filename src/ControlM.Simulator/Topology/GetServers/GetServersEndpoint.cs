using ControlM.Client.Operations;
using ControlM.Simulator.Operations;
using ControlM.Simulator.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ControlM.Simulator.Topology.GetServers;

internal static class GetServersEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapOperation(Operation.GetServers, Answer);

    private static IResult Answer() => Results.Json(Array.Empty<object>(), SimulatorJson.Options);
}
