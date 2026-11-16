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
            (HttpRequest request, CancellationToken cancellationToken) => AnswerAsync(request, credentials, cancellationToken))
            .WithMetadata(Operation);

    private static async Task<IResult> AnswerAsync(HttpRequest request, CredentialStore credentials, CancellationToken cancellationToken)
    {
        LoginCredentials? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<LoginCredentials>(request.Body, SimulatorJson.Options, cancellationToken);
        }
        catch (JsonException)
        {
            return CredentialFailureEndpoint.Answer(CredentialFailure.InvalidLoginBody());
        }

        var outcome = credentials.Login(body?.Username, body?.Password);
        if (outcome.Failure is { } failure)
        {
            return CredentialFailureEndpoint.Answer(failure);
        }

        return Results.Json(LoginResponse.Success(body!.Username, outcome.Token!), SimulatorJson.Options);
    }
}
