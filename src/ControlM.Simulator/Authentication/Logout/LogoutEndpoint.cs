using ControlM.Client.Authentication.ApiKey;
using ControlM.Client.Operations;
using ControlM.Simulator.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace ControlM.Simulator.Authentication.Logout;

internal static class LogoutEndpoint
{
    private static Operation Operation => Operation.Logout;

    public static void Map(IEndpointRouteBuilder routes, CredentialStore credentials) =>
        routes.MapMethods(
            Operation.Path,
            [Operation.Method.Method],
            ([FromHeader(Name = ApiKeyScheme.HeaderName)] string? apiKey, [FromHeader(Name = "Authorization")] string? authorization) =>
                Answer(credentials.Logout(apiKey, authorization)));

    private static IResult Answer(LogoutOutcome outcome)
    {
        if (outcome.Failure is { } failure)
        {
            return CredentialFailureEndpoint.Answer(failure);
        }

        return Results.Json(LogoutResponse.Success(outcome.Session!), SimulatorJson.Options);
    }
}
