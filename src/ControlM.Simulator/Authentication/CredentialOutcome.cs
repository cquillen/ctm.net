namespace ControlM.Simulator.Authentication;

internal enum CredentialOutcome
{
    Accepted,
    NoCredential,
    BothCredentialHeaders,
    InvalidOrExpiredToken,
    UnknownApiKey,
}
