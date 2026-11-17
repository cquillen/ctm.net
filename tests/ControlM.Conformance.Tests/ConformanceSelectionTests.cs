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
    public void An_endpoint_with_a_user_name_and_password_selects_a_real_server_with_session_login()
    {
        var selection = ConformanceSelection.From(new Dictionary<string, string>
        {
            ["CTM_CONFORMANCE_ENDPOINT"] = "https://host1.test:8443/automation-api",
            ["CTM_CONFORMANCE_USERNAME"] = "USER1",
            ["CTM_CONFORMANCE_PASSWORD"] = "PASSWORD1",
        });

        selection.ShouldBe(new ConformanceSelection.RealServer(
            new Uri("https://host1.test:8443/automation-api"),
            new ConformanceCredential.SessionLogin("USER1", "PASSWORD1")));
    }

    [Test]
    [Arguments("CTM_CONFORMANCE_USERNAME", "CTM_CONFORMANCE_PASSWORD", null)]
    [Arguments("CTM_CONFORMANCE_PASSWORD", "CTM_CONFORMANCE_USERNAME", null)]
    [Arguments("CTM_CONFORMANCE_USERNAME", "CTM_CONFORMANCE_PASSWORD", " ")]
    [Arguments("CTM_CONFORMANCE_PASSWORD", "CTM_CONFORMANCE_USERNAME", "")]
    public void Half_a_session_login_is_invalid_naming_the_missing_variable(string given, string missing, string? blank)
    {
        var environment = new Dictionary<string, string>
        {
            ["CTM_CONFORMANCE_ENDPOINT"] = "https://host1.test:8443/automation-api",
            [given] = "VALUE1",
        };
        if (blank is not null)
        {
            environment[missing] = blank;
        }

        var selection = ConformanceSelection.From(environment);

        var error = selection.ShouldBeOfType<ConformanceSelection.Invalid>().Errors.ShouldHaveSingleItem();
        error.ShouldContain(missing);
        error.ShouldNotContain("CTM_CONFORMANCE_API_KEY");
    }

    [Test]
    [Arguments("CTM_CONFORMANCE_USERNAME")]
    [Arguments("CTM_CONFORMANCE_PASSWORD")]
    [Arguments("CTM_CONFORMANCE_USERNAME", "CTM_CONFORMANCE_PASSWORD")]
    public void An_api_key_with_a_session_login_variable_is_invalid_naming_the_conflict(params string[] sessionVariables)
    {
        var environment = new Dictionary<string, string>
        {
            ["CTM_CONFORMANCE_ENDPOINT"] = "https://host1.test:8443/automation-api",
            ["CTM_CONFORMANCE_API_KEY"] = "APIKEY1",
        };
        foreach (var name in sessionVariables)
        {
            environment[name] = "VALUE1";
        }

        var selection = ConformanceSelection.From(environment);

        var error = selection.ShouldBeOfType<ConformanceSelection.Invalid>().Errors.ShouldHaveSingleItem();
        error.ShouldContain("CTM_CONFORMANCE_API_KEY");
        sessionVariables.ShouldAllBe(name => error.Contains(name, StringComparison.Ordinal));
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

    [Test]
    public void Variable_names_are_matched_regardless_of_case()
    {
        var selection = ConformanceSelection.From(new Dictionary<string, string>
        {
            ["ctm_conformance_endpoint"] = "https://host1.test:8443/automation-api",
            ["Ctm_Conformance_Api_Key"] = "APIKEY1",
        });

        selection.ShouldBe(new ConformanceSelection.RealServer(
            new Uri("https://host1.test:8443/automation-api"),
            new ConformanceCredential.ApiKey("APIKEY1")));
    }

    [Test]
    [Arguments("")]
    [Arguments(" 	")]
    public void A_blank_api_key_counts_as_missing(string key)
    {
        var selection = ConformanceSelection.From(new Dictionary<string, string>
        {
            ["CTM_CONFORMANCE_ENDPOINT"] = "https://host1.test:8443/automation-api",
            ["CTM_CONFORMANCE_API_KEY"] = key,
        });

        selection.ShouldBeOfType<ConformanceSelection.Invalid>()
            .Errors.ShouldHaveSingleItem().ShouldContain("CTM_CONFORMANCE_API_KEY");
    }

    [Test]
    [Arguments(" ")]
    [Arguments("/automation-api")]
    [Arguments("host1.test/automation-api")]
    [Arguments("ftp://host1.test/automation-api")]
    [Arguments("file:///automation-api")]
    public void An_endpoint_that_is_not_an_absolute_http_uri_is_invalid_naming_the_endpoint(string endpoint)
    {
        var selection = ConformanceSelection.From(new Dictionary<string, string>
        {
            ["CTM_CONFORMANCE_ENDPOINT"] = endpoint,
            ["CTM_CONFORMANCE_API_KEY"] = "APIKEY1",
        });

        selection.ShouldBeOfType<ConformanceSelection.Invalid>()
            .Errors.ShouldHaveSingleItem().ShouldContain("CTM_CONFORMANCE_ENDPOINT");
    }

    [Test]
    public void No_error_contains_the_api_key()
    {
        var selection = ConformanceSelection.From(new Dictionary<string, string>
        {
            ["CTM_CONFORMANCE_ENDPOINT"] = "ftp://host1.test/automation-api",
            ["CTM_CONFORMANCE_API_KEY"] = "KEYVALUE1",
        });

        selection.ShouldBeOfType<ConformanceSelection.Invalid>()
            .Errors.ShouldAllBe(error => !error.Contains("KEYVALUE1", StringComparison.Ordinal));
    }

    [Test]
    public void No_error_contains_the_password()
    {
        var selection = ConformanceSelection.From(new Dictionary<string, string>
        {
            ["CTM_CONFORMANCE_ENDPOINT"] = "ftp://host1.test/automation-api",
            ["CTM_CONFORMANCE_API_KEY"] = "APIKEY1",
            ["CTM_CONFORMANCE_USERNAME"] = "USER1",
            ["CTM_CONFORMANCE_PASSWORD"] = "PASSWORDVALUE1",
        });

        selection.ShouldBeOfType<ConformanceSelection.Invalid>()
            .Errors.ShouldAllBe(error => !error.Contains("PASSWORDVALUE1", StringComparison.Ordinal));
    }

    [Test]
    public void A_real_server_selection_does_not_print_its_api_key()
    {
        var selection = ConformanceSelection.From(new Dictionary<string, string>
        {
            ["CTM_CONFORMANCE_ENDPOINT"] = "https://host1.test:8443/automation-api",
            ["CTM_CONFORMANCE_API_KEY"] = "KEYVALUE1",
        });

        selection.ToString().ShouldNotContain("KEYVALUE1");
    }
}
