using System.Net;
using System.Text.Json;
using ControlM.Client.Operations;
using ControlM.Client.Serialization;

namespace ControlM.Client.Errors;

internal static class ApiFailure
{
    public static async Task<ControlMApiException> ReadAsync(
        HttpResponseMessage response,
        HttpMethod method,
        string path,
        Operation? operation,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var (errors, responseBody) = ContentOf(body);
        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new ControlMAuthenticationException(errors, responseBody, method, path, operation),
            HttpStatusCode.NotFound => new ControlMNotFoundException(errors, responseBody, method, path, operation),
            _ => new ControlMApiException(response.StatusCode, errors, responseBody, method, path, operation),
        };
    }

    private static (IReadOnlyList<ControlMError> Errors, string? ResponseBody) ContentOf(string body) =>
        ErrorsOf(body) is { } errors ? (errors, null) : ([], body);

    private static IReadOnlyList<ControlMError>? ErrorsOf(string body)
    {
        try
        {
            var errors = JsonSerializer.Deserialize<ErrorList>(body, ControlMJson.Options)?.Errors;
            return errors is not null && errors.All(e => e.Message is not null) ? errors : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
