using System.Net;
using ControlM.Client.Errors;
using ControlM.Simulator.Errors;

namespace ControlM.Simulator.Authentication;

internal sealed record CredentialFailure(HttpStatusCode StatusCode, ErrorList Body)
{
    public static CredentialFailure NoCredential() => new(HttpStatusCode.Unauthorized, ErrorEnvelope.NoCredential());

    public static CredentialFailure BothCredentialHeaders() =>
        new(HttpStatusCode.Unauthorized, ErrorEnvelope.BothCredentialHeaders());

    public static CredentialFailure InvalidOrExpiredToken() =>
        new(HttpStatusCode.Unauthorized, ErrorEnvelope.InvalidOrExpiredToken());

    public static CredentialFailure UnknownApiKey() => new(HttpStatusCode.BadRequest, ErrorEnvelope.UnknownApiKey());

    public static CredentialFailure IncorrectUsernameOrPassword() =>
        new(HttpStatusCode.Unauthorized, ErrorEnvelope.IncorrectUsernameOrPassword());

    // A real server answered 500 for a known user's wrong password, seen for one user only (requirements section 4.2).
    public static CredentialFailure WrongPassword() => new(HttpStatusCode.InternalServerError, ErrorEnvelope.LoginFailed());
}
