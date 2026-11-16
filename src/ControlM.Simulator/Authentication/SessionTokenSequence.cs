using System.Globalization;

namespace ControlM.Simulator.Authentication;

// Kept apart from ApiKeySequence: a token and an API key are different credentials, and seeding keys must not change
// which token a login returns.
internal sealed class SessionTokenSequence
{
    private int _last;

    public string Next() => string.Create(CultureInfo.InvariantCulture, $"SIMTOKEN{Interlocked.Increment(ref _last)}");
}
