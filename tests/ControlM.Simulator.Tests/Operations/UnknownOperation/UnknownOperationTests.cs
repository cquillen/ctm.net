using System.Net;
using ControlM.Fixtures;
using ControlM.Simulator.Hosting;
using Shouldly;

namespace ControlM.Simulator.Tests.Operations.UnknownOperation;

public sealed class UnknownOperationTests
{
    private static readonly string Fixture = JsonFixture.Load("Operations", "UnknownOperation", "UnknownOperation.get.json");

    [Test]
    public async Task A_get_to_an_unknown_path_answers_404_with_the_error_list()
    {
        await using var simulator = await ControlMSimulator.StartAsync();
        using var http = simulator.CreateHttpClient();

        using var response = await http.GetAsync(new Uri("/automation-api/no/such/path", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).ShouldEqualJson(Fixture);
    }
}
