using System.Net;
using ControlM.Fixtures;
using ControlM.Simulator.Hosting;
using Shouldly;

namespace ControlM.Simulator.Tests.Operations;

public sealed class WrongMethodTests
{
    private static string Fixture => JsonFixture.Load("Operations", "UnknownOperation", "UnknownOperation.wrong-method.json");

    [Test]
    public async Task A_wrong_method_on_a_real_path_answers_405_with_the_methods_it_serves()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        using var http = simulator.CreateHttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Patch, new Uri("/automation-api/config/servers", UriKind.Relative));

        using var response = await http.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
        response.Content.Headers.Allow.ShouldBe(["GET"]);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(Fixture);
    }

    [Test]
    public async Task A_seeded_key_gets_the_same_405_as_no_credential()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        simulator.Credentials.AddApiKey("APIKEY1");
        using var http = simulator.CreateHttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Patch, new Uri("/automation-api/config/servers", UriKind.Relative));
        request.Headers.Add("x-api-key", "APIKEY1");

        using var response = await http.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
        response.Content.Headers.Allow.ShouldBe(["GET"]);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(Fixture);
    }

    [Test]
    public async Task The_query_string_is_left_out_of_the_reported_uri()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        using var http = simulator.CreateHttpClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            new Uri("/automation-api/config/servers?server=SERVER1", UriKind.Relative));

        using var response = await http.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(Fixture);
    }
}
