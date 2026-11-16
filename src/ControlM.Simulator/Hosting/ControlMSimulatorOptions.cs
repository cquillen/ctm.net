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

    /// <summary>
    /// How long a session may go unused before its token is refused. Every request authenticated with the token
    /// restarts this time. Defaults to 30 minutes, as BMC documents.
    /// </summary>
    public TimeSpan SessionLifetime { get; set; }
}
