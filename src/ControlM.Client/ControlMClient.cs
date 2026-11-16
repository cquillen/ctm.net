using System.Net.Http.Json;
using ControlM.Client.Serialization;

namespace ControlM.Client;

internal sealed class ControlMClient(HttpClient httpClient, ControlMClientOptions options) : IControlMClient
{
    private readonly string _endpoint = options.Endpoint!.AbsoluteUri.TrimEnd('/');

    public async Task<T> SendAsync<T>(HttpMethod method, string path, object? body = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(method, RequestUri(path))
        {
            Content = body is null ? null : JsonContent.Create(body, options: ControlMJson.Options),
        };
        using var response = await httpClient.SendAsync(request, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<T>(ControlMJson.Options, cancellationToken))!;
    }

    public Task SendAsync(HttpMethod method, string path, object? body = null, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    private Uri RequestUri(string path) => new($"{_endpoint}/{path.TrimStart('/')}");
}
