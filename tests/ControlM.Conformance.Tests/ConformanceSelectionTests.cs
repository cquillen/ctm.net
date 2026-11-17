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
    public void Variables_without_the_prefix_are_ignored()
    {
        var selection = ConformanceSelection.From(new Dictionary<string, string>
        {
            ["PATH"] = "/usr/bin",
            ["CTM_ENDPOINT"] = "https://host1.test:8443/automation-api",
        });

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
            .Errors.ShouldContain(error => error.Contains("CTM_CONFORMANCE_ENDPOINT", StringComparison.Ordinal));
    }

    [Test]
    public void An_endpoint_with_an_api_key_selects_a_real_server_with_that_key()
    {
        var selection = ConformanceSelection.From(new Dictionary<string, string>
        {
            ["CTM_CONFORMANCE_ENDPOINT"] = "https://host1.test:8443/automation-api",
            ["CTM_CONFORMANCE_API_KEY"] = "APIKEY1",
        });

        selection.ShouldBe(new ConformanceSelection.RealServer(
            new Uri("https://host1.test:8443/automation-api"),
            new ConformanceCredential.ApiKey("APIKEY1")));
    }

    [Test]
    public void An_endpoint_without_a_credential_is_invalid_naming_the_credential_variables()
    {
        var selection = ConformanceSelection.From(new Dictionary<string, string>
        {
            ["CTM_CONFORMANCE_ENDPOINT"] = "https://host1.test:8443/automation-api",
        });

        var error = selection.ShouldBeOfType<ConformanceSelection.Invalid>().Errors.ShouldHaveSingleItem();
        error.ShouldContain("CTM_CONFORMANCE_API_KEY");
        error.ShouldContain("CTM_CONFORMANCE_USERNAME");
        error.ShouldContain("CTM_CONFORMANCE_PASSWORD");
    }
}
