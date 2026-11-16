namespace ControlM.Simulator.Hosting;

/// <summary>
/// The settings of a simulator instance, set in the callback given to <see cref="ControlMSimulator.StartAsync"/>.
/// </summary>
public sealed class ControlMSimulatorOptions
{
    /// <summary>
    /// The source of every time the simulator reads. Defaults to <see cref="TimeProvider.System"/>; a test that
    /// controls time passes a fake one.
    /// </summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;
}
