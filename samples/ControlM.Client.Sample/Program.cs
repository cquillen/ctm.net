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
    private const string _getServersStep = "GET /config/servers";

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

        var session = new SessionWatch();
        var passed = await CheckAsync(endpoint, username, password, session);
        if (!session.Opened)
        {
            Report("SKIP", "logout", "no session was opened");
            return 1;
        }

        // The client swallows every failure of the logout it sends on disposal, so there is no failure to report.
        Report("PASS", "logout", "the service provider was disposed");
        return passed ? 0 : 1;
    }

    private static async Task<bool> CheckAsync(Uri endpoint, string username, string password, SessionWatch session)
    {
        var services = new ServiceCollection();
        services.AddControlMClient(o =>
        {
            o.Endpoint = endpoint;
            o.UseSessionLogin(username, password);
        }).AddHttpMessageHandler(session.CreateHandler);
        await using var provider = services.BuildServiceProvider();

        IControlMClient client;
        try
        {
            client = provider.GetRequiredService<IControlMClient>();
        }
        catch (OptionsValidationException exception)
        {
            Report("FAIL", "configure", exception.Message);
            return false;
        }

        return await GetServersAsync(client);
    }

    private static async Task<bool> GetServersAsync(IControlMClient client)
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
            Report("SKIP", _getServersStep, "not reached");
            return false;
        }
        catch (ControlMApiException exception)
        {
            Report("PASS", "login");
            Report("FAIL", _getServersStep, Describe(exception));
            return false;
        }
        catch (HttpRequestException exception)
        {
            Report("FAIL", $"login or {_getServersStep}", $"no response: {exception.HttpRequestError}");
            return false;
        }
        catch (TaskCanceledException)
        {
            Report("FAIL", $"login or {_getServersStep}", "no response: timed out");
            return false;
        }
    }

    private static bool ReportServers(JsonElement servers)
    {
        if (servers.ValueKind != JsonValueKind.Array)
        {
            Report("FAIL", _getServersStep, $"the response is a JSON {servers.ValueKind}, not an array");
            return false;
        }

        Report("PASS", _getServersStep, $"{servers.GetArrayLength()} servers");
        return true;
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

    // The client opens its session inside its first call, so only the login response itself shows whether one opened.
    private sealed class SessionWatch
    {
        public bool Opened { get; private set; }

        public DelegatingHandler CreateHandler() => new Watcher(this);

        private sealed class Watcher(SessionWatch session) : DelegatingHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = await base.SendAsync(request, cancellationToken);
                if (IsLogin(request) && response.IsSuccessStatusCode)
                {
                    session.Opened = true;
                }

                return response;
            }

            private static bool IsLogin(HttpRequestMessage request) =>
                request.Method == Operation.Login.Method
                && request.RequestUri?.AbsolutePath.EndsWith(Operation.Login.Path, StringComparison.Ordinal) == true;
        }
    }
}
