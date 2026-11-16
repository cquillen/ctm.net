using System.Text.Json;
using ControlM.Client.Authentication.Login;
using ControlM.Client.Operations;
using ControlM.Simulator.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ControlM.Simulator.Authentication.Login;

internal static class LoginEndpoint
{
    private static Operation Operation => Operation.Login;

    public static void Map(IEndpointRouteBuilder routes, CredentialStore credentials) =>
        routes.MapMethods(
            Operation.Path,
            [Operation.Method.Method],
            (HttpRequest request, CancellationToken cancellationToken) => AnswerAsync(request, credentials, cancellationToken));

    private static async Task<IResult> AnswerAsync(HttpRequest request, CredentialStore credentials, CancellationToken cancellationToken)
    {
        var body = await JsonSerializer.DeserializeAsync<LoginCredentials>(request.Body, SimulatorJson.Options, cancellationToken);
        var outcome = credentials.Login(body?.Username, body?.Password);
        if (outcome.Failure is { } failure)
        {
            return Results.Json(failure.Body, SimulatorJson.Options, statusCode: (int)failure.StatusCode);
        }

        return Results.Json(LoginResponse.Success(body!.Username, outcome.Token!), SimulatorJson.Options);
    }
}
