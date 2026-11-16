using ControlM.Simulator.Authentication;
using ControlM.Simulator.Errors;
using ControlM.Simulator.Serialization;
using Microsoft.AspNetCore.Http;

namespace ControlM.Simulator.Hosting;

internal sealed class CredentialFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var headers = context.HttpContext.Request.Headers;
        var outcome = CredentialCheck.Check(
            headers[CredentialCheck.ApiKeyHeader].FirstOrDefault(),
            headers.Authorization.Count > 0);
        if (outcome != CredentialOutcome.Accepted)
        {
            return Results.Json(ErrorEnvelope.NoCredential(), SimulatorJson.Options, statusCode: StatusCodes.Status401Unauthorized);
        }

        return await next(context);
    }
}
