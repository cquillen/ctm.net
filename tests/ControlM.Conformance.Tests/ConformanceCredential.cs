namespace ControlM.Conformance.Tests;

internal abstract record ConformanceCredential
{
    public sealed record ApiKey(string Key) : ConformanceCredential
    {
        public override string ToString() => nameof(ApiKey);
    }
}
