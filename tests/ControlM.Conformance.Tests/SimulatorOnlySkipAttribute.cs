using ControlM.Conformance.Tests;

[assembly: SimulatorOnlySkip]

namespace ControlM.Conformance.Tests;

[AttributeUsage(AttributeTargets.Assembly)]
internal sealed class SimulatorOnlySkipAttribute() : SkipAttribute($"{Category} tests do not run against a real server.")
{
    internal const string Category = "SimulatorOnly";

    public override Task<bool> ShouldSkip(TestRegisteredContext context) =>
        Task.FromResult(
            context.TestDetails.Categories.Contains(Category)
            && ConformanceTarget.Selection is not ConformanceSelection.Simulator);

    internal static string ReasonFor(ConformanceSelection selection) =>
        selection is ConformanceSelection.Invalid invalid
            ? $"{Category} tests do not run while the {ConformanceSelection.Prefix} variables are invalid:{Environment.NewLine}{string.Join(Environment.NewLine, invalid.Errors)}"
            : $"{Category} tests do not run against the valid real server the {ConformanceSelection.Prefix} variables select.";

    protected override string GetSkipReason(TestRegisteredContext context) => ReasonFor(ConformanceTarget.Selection);
}
