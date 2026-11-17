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

    [Test]
    [Arguments("CTM_CONFORMANCE_ENDPIONT")]
    [Arguments("CTM_CONFORMANCE_SERVER")]
    [Arguments("CTM_CONFORMANCE_HOST")]
    [Arguments("CTM_CONFORMANCE_FOLDER")]
    public void Any_prefixed_variable_without_an_endpoint_is_invalid_naming_the_endpoint(string name)
    {
        var selection = ConformanceSelection.From(new Dictionary<string, string> { [name] = "VALUE1" });

        selection.ShouldBeOfType<ConformanceSelection.Invalid>()
            .Errors.ShouldHaveSingleItem().ShouldContain("CTM_CONFORMANCE_ENDPOINT");
    }
}
