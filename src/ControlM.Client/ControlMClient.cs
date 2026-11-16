using System.Net.Http.Json;
using ControlM.Client.Errors;
using ControlM.Client.Serialization;
using Microsoft.Extensions.Options;

namespace ControlM.Client;

internal sealed class ControlMClient(HttpClient httpClient, IOptions<ControlMClientOptions> options) : IControlMClient
{
    private readonly string _endpoint = options.Value.Endpoint!.AbsoluteUri.TrimEnd('/');

    public async Task<T> SendAsync<T>(HttpMethod method, string path, object? body = null, CancellationToken cancellationToken = default)
    {
        using var response = await SendCoreAsync(method, path, body, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<T>(ControlMJson.Options, cancellationToken))!;
    }

    public async Task SendAsync(HttpMethod method, string path, object? body = null, CancellationToken cancellationToken = default)
    {
        using var response = await SendCoreAsync(method, path, body, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendCoreAsync(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var request = new HttpRequestMessage(method, RequestUri(path))
        {
            Content = body is null ? null : JsonContent.Create(body, options: ControlMJson.Options),
        };
        var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        using var failure = response;
        throw await ApiFailure.ReadAsync(failure, method, path, operation: null, cancellationToken);
    }

    private Uri RequestUri(string path) => new($"{_endpoint}/{path.TrimStart('/')}");
}
