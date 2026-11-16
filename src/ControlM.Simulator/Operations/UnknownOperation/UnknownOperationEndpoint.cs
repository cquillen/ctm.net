using ControlM.Simulator.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ControlM.Simulator.Operations.UnknownOperation;

internal static class UnknownOperationEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder routes) => MapForEveryMethod(routes);

    // Only GET was observed on a real server; requirements section 4.8 assumes every other method gets the same 404.
    private static RouteHandlerBuilder MapForEveryMethod(IEndpointRouteBuilder routes) => routes.MapFallback("{**path}", Answer);

    private static IResult Answer(HttpRequest request) =>
        Results.Json(
            UnknownPath.Response(request.Path.Value ?? string.Empty),
            SimulatorJson.Options,
            statusCode: StatusCodes.Status404NotFound);
}
