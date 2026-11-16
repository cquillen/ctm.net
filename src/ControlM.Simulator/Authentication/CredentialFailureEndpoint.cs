using ControlM.Simulator.Serialization;
using Microsoft.AspNetCore.Http;

namespace ControlM.Simulator.Authentication;

internal static class CredentialFailureEndpoint
{
    public static IResult Answer(CredentialFailure failure) =>
        Results.Json(failure.Body, SimulatorJson.Options, statusCode: (int)failure.StatusCode);
}
