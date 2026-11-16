using System.Net;
using System.Net.Http.Json;
using ControlM.Client.Serialization;

namespace ControlM.Client.Errors;

internal static class ApiFailure
{
    public static async Task<ControlMApiException> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var errorList = await response.Content.ReadFromJsonAsync<ErrorList>(ControlMJson.Options, cancellationToken);
        var errors = errorList!.Errors!;
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new ControlMNotFoundException(errors);
        }

        return new ControlMApiException(response.StatusCode, errors);
    }
}
