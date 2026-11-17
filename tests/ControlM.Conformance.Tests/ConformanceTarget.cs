using System.Collections;
using ControlM.Client;
using ControlM.Simulator.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ControlM.Conformance.Tests;

internal sealed class ConformanceTarget : IAsyncDisposable
{
    private static readonly Lazy<ConformanceSelection> _selection =
        new(() => ConformanceSelection.From(ReadEnvironment()));

    private readonly IAsyncDisposable _owner;

    private ConformanceTarget(IControlMClient client, IAsyncDisposable owner)
    {
        Client = client;
        _owner = owner;
    }

    public IControlMClient Client { get; }

    public static async Task<ConformanceTarget> StartAsync()
    {
        var selection = _selection.Value;
        if (selection is ConformanceSelection.Invalid invalid)
        {
            throw new InvalidOperationException(
                $"The conformance target is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, invalid.Errors)}");
        }

        if (selection is ConformanceSelection.RealServer realServer)
        {
            return ConnectTo(realServer);
        }

        var simulator = await ControlMSimulator.StartAsync();
        return new ConformanceTarget(simulator.CreateClient(), simulator);
    }

    public ValueTask DisposeAsync() => _owner.DisposeAsync();

    private static ConformanceTarget ConnectTo(ConformanceSelection.RealServer realServer)
    {
        var services = new ServiceCollection();
        services.AddControlMClient(o =>
        {
            o.Endpoint = realServer.Endpoint;
            realServer.Credential.Apply(o);
        });
        var provider = services.BuildServiceProvider();
        return new ConformanceTarget(provider.GetRequiredService<IControlMClient>(), provider);
    }

    private static Dictionary<string, string> ReadEnvironment() =>
        Environment.GetEnvironmentVariables()
            .Cast<DictionaryEntry>()
            .ToDictionary(entry => (string)entry.Key, entry => entry.Value as string ?? string.Empty);
}
