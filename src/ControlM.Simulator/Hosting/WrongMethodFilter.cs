using ControlM.Simulator.Operations;
using ControlM.Simulator.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;

namespace ControlM.Simulator.Hosting;

// The fallback route matches every method, so routing never produces its own 405: a wrong method on a real path
// reaches the fallback, and this filter tells it apart from an unknown path by the endpoints registered for the path.
internal sealed class WrongMethodFilter(EndpointDataSource endpoints) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.HttpContext.Request;
        var served = MethodsServedAt(request.Path);
        if (served.Count == 0)
        {
            return await next(context);
        }

        context.HttpContext.Response.Headers.Allow = string.Join(", ", served);
        return Results.Json(
            WrongMethod.Response(request.Path.Value ?? string.Empty),
            SimulatorJson.Options,
            statusCode: StatusCodes.Status405MethodNotAllowed);
    }

    private List<string> MethodsServedAt(PathString path) =>
        [.. endpoints.Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => Matches(endpoint, path))
            .SelectMany(endpoint => endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? [])
            .Distinct(StringComparer.OrdinalIgnoreCase)];

    private static bool Matches(RouteEndpoint endpoint, PathString path) =>
        new TemplateMatcher(new RouteTemplate(endpoint.RoutePattern), []).TryMatch(path, []);
}
