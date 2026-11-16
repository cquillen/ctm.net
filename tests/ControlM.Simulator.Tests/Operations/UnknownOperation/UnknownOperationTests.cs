using System.Net;
using ControlM.Fixtures;
using ControlM.Simulator.Hosting;
using Shouldly;

namespace ControlM.Simulator.Tests.Operations.UnknownOperation;

public sealed class UnknownOperationTests
{
    private static string Fixture => JsonFixture.Load("Operations", "UnknownOperation", "UnknownOperation.get.json");

    [Test]
    public async Task A_get_to_an_unknown_path_answers_404_with_the_error_list()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        using var http = simulator.CreateHttpClient();

        using var response = await http.GetAsync(new Uri("/automation-api/no/such/path", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(Fixture);
    }

    [Test]
    [Arguments("POST")]
    [Arguments("PUT")]
    [Arguments("PATCH")]
    [Arguments("DELETE")]
    public async Task Another_method_on_an_unknown_path_gets_the_same_404(string method)
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        using var http = simulator.CreateHttpClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), new Uri("/automation-api/no/such/path", UriKind.Relative));

        using var response = await http.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(Fixture);
    }

    [Test]
    public async Task The_query_string_is_left_out_of_the_reported_uri()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        using var http = simulator.CreateHttpClient();

        using var response = await http.GetAsync(new Uri("/automation-api/no/such/path?server=SERVER1", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(Fixture);
    }

    [Test]
    public async Task A_request_with_a_credential_gets_the_same_404_as_one_without()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        using var http = simulator.CreateHttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/automation-api/no/such/path", UriKind.Relative));
        request.Headers.Add("x-api-key", "APIKEY1");

        using var response = await http.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(Fixture);
    }

    [Test]
    public async Task A_path_outside_the_api_gets_the_same_404_with_its_whole_path()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        using var http = simulator.CreateHttpClient();

        using var response = await http.GetAsync(new Uri("/no/such/path", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(Fixture);
    }

    [Test]
    public async Task The_bare_prefix_is_outside_the_api_and_reports_itself_whole()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        using var http = simulator.CreateHttpClient();

        using var response = await http.GetAsync(new Uri("/automation-api", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(
            JsonFixture.Load("Operations", "UnknownOperation", "UnknownOperation.bare-prefix.json"));
    }
}
