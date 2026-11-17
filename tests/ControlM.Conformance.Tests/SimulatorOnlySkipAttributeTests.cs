using Shouldly;

namespace ControlM.Conformance.Tests;

public sealed class SimulatorOnlySkipAttributeTests
{
    [Test]
    public void The_reason_for_a_real_server_says_the_variables_select_a_valid_one_without_its_endpoint()
    {
        var selection = new ConformanceSelection.RealServer(
            new Uri("https://host1.test:8443/automation-api"),
            new ConformanceCredential.ApiKey("APIKEY1"));

        var reason = SimulatorOnlySkipAttribute.ReasonFor(selection);

        reason.ShouldContain("valid real server");
        reason.ShouldNotContain("host1.test");
    }

    [Test]
    public void The_reason_for_invalid_variables_says_so_and_gives_their_errors()
    {
        var selection = new ConformanceSelection.Invalid(["ERROR1", "ERROR2"]);

        var reason = SimulatorOnlySkipAttribute.ReasonFor(selection);

        reason.ShouldContain("invalid");
        reason.ShouldContain("ERROR1");
        reason.ShouldContain("ERROR2");
    }
}
