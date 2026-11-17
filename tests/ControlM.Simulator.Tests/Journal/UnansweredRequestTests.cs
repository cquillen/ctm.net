using System.Text;
using ControlM.Client.Operations;
using ControlM.Simulator.Hosting;
using Shouldly;

namespace ControlM.Simulator.Tests.Journal;

public sealed class UnansweredRequestTests
{
    [Test]
    public async Task A_request_whose_endpoint_throws_before_answering_is_recorded_with_no_status()
    {
        await using var simulator = await ControlMSimulator.StartAsync(o => o.TimeProvider = new ClockThatFailsAfterItsFirstReading());
        simulator.Credentials.AddUser("USER1", "PASSWORD1");
        using var http = simulator.CreateHttpClient();

        await LoginWhateverTheHostDoesWithTheExceptionAsync(http);

        var entry = simulator.Journal.Entries.ShouldHaveSingleItem();
        entry.Operation.ShouldBe(Operation.Login);
        entry.Status.ShouldBeNull();
    }

    // What reaches the client depends on the host's environment: the developer exception page answers 500, and
    // without it the test server throws the exception from the call.
    private static async Task LoginWhateverTheHostDoesWithTheExceptionAsync(HttpClient http)
    {
        try
        {
            using var response = await http.PostAsync(
                new Uri("/automation-api/session/login", UriKind.Relative),
                new StringContent("""{"username":"USER1","password":"PASSWORD1"}""", Encoding.UTF8, "application/json"));
        }
        catch (InvalidOperationException)
        {
        }
    }

    // The journal reads the clock when the request arrives; the login reads it again to start the session, and that
    // reading throws out of the endpoint before any response is written.
    private sealed class ClockThatFailsAfterItsFirstReading : TimeProvider
    {
        private int _readings;

        public override DateTimeOffset GetUtcNow() =>
            Interlocked.Increment(ref _readings) == 1
                ? new DateTimeOffset(2026, 3, 14, 9, 26, 53, TimeSpan.Zero)
                : throw new InvalidOperationException("The clock failed.");
    }
}
