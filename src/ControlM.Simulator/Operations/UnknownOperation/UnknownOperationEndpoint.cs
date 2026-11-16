using ControlM.Simulator.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ControlM.Simulator.Operations.UnknownOperation;

internal static class UnknownOperationEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => MapForEveryMethod(routes, ApiPath.Base + "/{**rest}");

    // Only GET was observed on a real server; requirements section 4.8 assumes every other method gets the same 404.
    private static void MapForEveryMethod(IEndpointRouteBuilder routes, string pattern) => routes.MapFallback(pattern, Answer);

    private static IResult Answer(HttpRequest request) =>
        Results.Json(
            UnknownPath.Response(request.Path.Value ?? string.Empty),
            SimulatorJson.Options,
            statusCode: StatusCodes.Status404NotFound);
}
