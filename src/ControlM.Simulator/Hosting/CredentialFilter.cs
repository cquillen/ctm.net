using ControlM.Client.Authentication.ApiKey;
using ControlM.Client.Errors;
using ControlM.Simulator.Authentication;
using ControlM.Simulator.Errors;
using ControlM.Simulator.Serialization;
using Microsoft.AspNetCore.Http;

namespace ControlM.Simulator.Hosting;

internal sealed class CredentialFilter(CredentialStore credentials) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var headers = context.HttpContext.Request.Headers;
        var outcome = credentials.Check(
            headers[ApiKeyScheme.HeaderName].FirstOrDefault(),
            headers.Authorization.Count > 0);
        if (outcome != CredentialOutcome.Accepted)
        {
            return Results.Json(FailureBody(outcome), SimulatorJson.Options, statusCode: StatusCodes.Status401Unauthorized);
        }

        return await next(context);
    }

    private static ErrorList FailureBody(CredentialOutcome outcome) => outcome switch
    {
        CredentialOutcome.NoCredential => ErrorEnvelope.NoCredential(),
        CredentialOutcome.InvalidOrExpiredToken => ErrorEnvelope.InvalidOrExpiredToken(),
        CredentialOutcome.UnknownApiKey => ErrorEnvelope.UnknownApiKey(),
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "The outcome is not a failure."),
    };
}
