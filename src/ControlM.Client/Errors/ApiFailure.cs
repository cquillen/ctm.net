using System.Net;
using System.Text.Json;
using ControlM.Client.Serialization;

namespace ControlM.Client.Errors;

internal static class ApiFailure
{
    public static async Task<ControlMApiException> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var errors = ErrorsOf(body);
        return errors is null
            ? Create(response.StatusCode, [], body)
            : Create(response.StatusCode, errors, responseBody: null);
    }

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

    private static ControlMApiException Create(HttpStatusCode statusCode, IReadOnlyList<ControlMError> errors, string? responseBody) =>
        statusCode == HttpStatusCode.NotFound
            ? new ControlMNotFoundException(errors, responseBody)
            : new ControlMApiException(statusCode, errors, responseBody);
}
