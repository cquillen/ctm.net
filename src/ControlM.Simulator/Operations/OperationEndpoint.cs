using ControlM.Client.Operations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ControlM.Simulator.Operations;

internal static class OperationEndpoint
{
    // The Operation travels as endpoint metadata, which is how the request journal knows what a request was routed to.
    public static RouteHandlerBuilder MapOperation(this IEndpointRouteBuilder routes, Operation operation, Delegate handler) =>
        routes.MapMethods(operation.Path, [operation.Method.Method], handler).WithMetadata(operation);
}
