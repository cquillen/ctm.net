using ControlM.Client.Authentication.ApiKey;
using ControlM.Simulator.Authentication;
using ControlM.Simulator.Serialization;
using Microsoft.AspNetCore.Http;

namespace ControlM.Simulator.Hosting;

internal sealed class CredentialFilter(CredentialStore credentials) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var headers = context.HttpContext.Request.Headers;
        var failure = credentials.Check(
            headers[ApiKeyScheme.HeaderName].FirstOrDefault(),
            headers.Authorization.FirstOrDefault());
        if (failure is not null)
        {
            return Results.Json(failure.Body, SimulatorJson.Options, statusCode: (int)failure.StatusCode);
        }

        return await next(context);
    }
}
