using ControlM.Client;

namespace ControlM.Conformance.Tests;

internal abstract record ConformanceCredential
{
    public abstract void Apply(ControlMClientOptions options);

    public sealed record ApiKey(string Key) : ConformanceCredential
    {
        public override void Apply(ControlMClientOptions options) => options.UseApiKey(Key);

        public override string ToString() => nameof(ApiKey);
    }

    public sealed record SessionLogin(string Username, string Password) : ConformanceCredential
    {
        public override void Apply(ControlMClientOptions options) => options.UseSessionLogin(Username, Password);
    }
}
