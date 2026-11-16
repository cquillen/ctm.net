namespace ControlM.Simulator.Authentication;

internal sealed class Session(DateTimeOffset openedAt)
{
    private readonly DateTimeOffset _lastUsed = openedAt;

    public bool IsLiveAt(DateTimeOffset now, TimeSpan lifetime) => now - _lastUsed < lifetime;
}
