using System.Text;
using System.Text.Json;
using ControlM.Client.Errors;
using ControlM.Client.Operations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ControlM.Client.Sample;

// What this checks, and what it must never print, is in docs/requirements.md section 7.
internal static class Program
{
    private const string GetServersStep = "GET /config/servers";

    private static async Task<int> Main()
    {
        var endpoint = ReadEndpoint();
        if (endpoint is null)
        {
            Report("FAIL", "endpoint", "not an absolute http or https address");
            return 1;
        }

        var username = Prompt("User name: ");
        var password = ReadPassword("Password: ");
        Console.WriteLine();

        var outcome = await CheckAsync(endpoint, username, password);
        if (outcome == Outcome.NoSession)
        {
            Report("SKIP", "logout", "no session was opened");
            return 1;
        }

        // The client swallows every failure of the logout it sends on disposal, so there is no failure to report.
        Report("PASS", "logout", "the service provider was disposed");
        return outcome == Outcome.Passed ? 0 : 1;
    }

    private enum Outcome
    {
        NoSession,
        Failed,
        Passed,
    }

    // Disposing the provider logs out when a session was opened.
    private static async Task<Outcome> CheckAsync(Uri endpoint, string username, string password)
    {
        var services = new ServiceCollection();
        services.AddControlMClient(o =>
        {
            o.Endpoint = endpoint;
            o.UseSessionLogin(username, password);
        });
        await using var provider = services.BuildServiceProvider();

        IControlMClient client;
        try
        {
            client = provider.GetRequiredService<IControlMClient>();
        }
        catch (OptionsValidationException exception)
        {
            Report("FAIL", "configure", exception.Message);
            return Outcome.NoSession;
        }

        return await GetServersAsync(client);
    }

    private static async Task<Outcome> GetServersAsync(IControlMClient client)
    {
        try
        {
            var servers = await client.SendAsync<JsonElement>(HttpMethod.Get, "/config/servers");
            Report("PASS", "login");
            return ReportServers(servers);
        }
        catch (ControlMApiException exception) when (exception.Operation == Operation.Login)
        {
            Report("FAIL", "login", Describe(exception));
            Report("SKIP", GetServersStep, "not reached");
            return Outcome.NoSession;
        }
        catch (ControlMApiException exception)
        {
            Report("PASS", "login");
            Report("FAIL", GetServersStep, Describe(exception));
            return Outcome.Failed;
        }
        catch (HttpRequestException exception)
        {
            Report("FAIL", $"login or {GetServersStep}", $"no response: {exception.HttpRequestError}");
            return Outcome.Failed;
        }
        catch (TaskCanceledException)
        {
            Report("FAIL", $"login or {GetServersStep}", "no response: timed out");
            return Outcome.Failed;
        }
    }

    private static Outcome ReportServers(JsonElement servers)
    {
        if (servers.ValueKind != JsonValueKind.Array)
        {
            Report("FAIL", GetServersStep, $"the response is a JSON {servers.ValueKind}, not an array");
            return Outcome.Failed;
        }

        Report("PASS", GetServersStep, $"{servers.GetArrayLength()} servers");
        return Outcome.Passed;
    }

    private static string Describe(ControlMApiException exception)
    {
        var message = exception.Errors.Count > 0 ? exception.Errors[0].Message : "no error message";
        return $"{(int)exception.StatusCode} {message}";
    }

    private static void Report(string result, string step, string? detail = null) =>
        Console.WriteLine(detail is null ? $"{result}  {step}" : $"{result}  {step}: {detail}");

    private static Uri? ReadEndpoint()
    {
        var text = Prompt("Endpoint, such as https://host1:8443/automation-api: ");
        if (!Uri.TryCreate(text, UriKind.Absolute, out var endpoint))
        {
            return null;
        }

        return endpoint.Scheme == Uri.UriSchemeHttps || endpoint.Scheme == Uri.UriSchemeHttp ? endpoint : null;
    }

    private static string Prompt(string prompt)
    {
        Console.Write(prompt);
        return Console.ReadLine()?.Trim() ?? string.Empty;
    }

    private static string ReadPassword(string prompt)
    {
        Console.Write(prompt);
        var password = new StringBuilder();
        for (var key = Console.ReadKey(intercept: true); key.Key != ConsoleKey.Enter; key = Console.ReadKey(intercept: true))
        {
            if (key.Key == ConsoleKey.Backspace)
            {
                password.Length = Math.Max(0, password.Length - 1);
                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                password.Append(key.KeyChar);
            }
        }

        Console.WriteLine();
        return password.ToString();
    }
}
