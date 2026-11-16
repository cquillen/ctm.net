using ControlM.Simulator.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ControlM.Simulator.Operations.UnknownOperation;

internal static class UnknownOperationEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet(ApiPath.Base + "/{**rest}", Answer);

    private static IResult Answer(HttpRequest request) =>
        Results.Json(
            UnknownPath.Response(request.Path.Value ?? string.Empty),
            SimulatorJson.Options,
            statusCode: StatusCodes.Status404NotFound);
}
