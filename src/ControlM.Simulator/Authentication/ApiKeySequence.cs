using System.Globalization;

namespace ControlM.Simulator.Authentication;

internal sealed class ApiKeySequence
{
    private int _last;

    public string Next() => string.Create(CultureInfo.InvariantCulture, $"SIMKEY{Interlocked.Increment(ref _last)}");
}
