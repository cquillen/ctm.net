using ControlM.Conformance.Tests;

[assembly: SimulatorOnlySkip]

namespace ControlM.Conformance.Tests;

[AttributeUsage(AttributeTargets.Assembly)]
internal sealed class SimulatorOnlySkipAttribute()
    : SkipAttribute("SimulatorOnly tests do not run against the real server the CTM_CONFORMANCE_ variables select.")
{
    private const string _category = "SimulatorOnly";

    public override Task<bool> ShouldSkip(TestRegisteredContext context) =>
        Task.FromResult(
            context.TestDetails.Categories.Contains(_category)
            && ConformanceTarget.Selection is not ConformanceSelection.Simulator);
}
