using Shouldly;

namespace ControlM.Conformance.Tests;

public sealed class ConformanceSelectionTests
{
    [Test]
    public void No_variables_select_the_simulator()
    {
        var selection = ConformanceSelection.From(new Dictionary<string, string>());

        selection.ShouldBeOfType<ConformanceSelection.Simulator>();
    }
}
