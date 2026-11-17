using System.Collections;
using ControlM.Client;
using ControlM.Simulator.Hosting;

namespace ControlM.Conformance.Tests;

internal sealed class ConformanceTarget : IAsyncDisposable
{
    private static readonly Lazy<ConformanceSelection> _selection =
        new(() => ConformanceSelection.From(ReadEnvironment()));

    private readonly ControlMSimulator _simulator;

    private ConformanceTarget(ControlMSimulator simulator)
    {
        _simulator = simulator;
        Client = simulator.CreateClient();
    }

    public static ConformanceSelection Selection => _selection.Value;

    public IControlMClient Client { get; }

    public static async Task<ConformanceTarget> StartAsync()
    {
        if (Selection is ConformanceSelection.Invalid invalid)
        {
            throw new InvalidOperationException(
                $"The conformance target is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, invalid.Errors)}");
        }

        return new ConformanceTarget(await ControlMSimulator.StartAsync());
    }

    public ValueTask DisposeAsync() => _simulator.DisposeAsync();

    private static Dictionary<string, string> ReadEnvironment() =>
        Environment.GetEnvironmentVariables()
            .Cast<DictionaryEntry>()
            .ToDictionary(entry => (string)entry.Key, entry => entry.Value as string ?? string.Empty);
}
