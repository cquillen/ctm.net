namespace ControlM.Simulator.Authentication;

internal sealed class Session(DateTimeOffset openedAt)
{
    private readonly Lock _lock = new();
    private DateTimeOffset _lastUsed = openedAt;

    public bool TryUse(DateTimeOffset now, TimeSpan lifetime)
    {
        lock (_lock)
        {
            if (now - _lastUsed >= lifetime)
            {
                return false;
            }

            _lastUsed = now;
            return true;
        }
    }
}
