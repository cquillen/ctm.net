using System.Net.Http.Json;
using ControlM.Client.Serialization;

namespace ControlM.Client;

internal sealed class ControlMClient(HttpClient httpClient, ControlMClientOptions options) : IControlMClient
{
    private readonly string _endpoint = options.Endpoint!.AbsoluteUri.TrimEnd('/');

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
        using var request = new HttpRequestMessage(method, RequestUri(path))
        {
            Content = body is null ? null : JsonContent.Create(body, options: ControlMJson.Options),
        };
        return await httpClient.SendAsync(request, cancellationToken);
    }

    private Uri RequestUri(string path) => new($"{_endpoint}/{path.TrimStart('/')}");
}
