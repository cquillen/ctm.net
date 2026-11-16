namespace ControlM.Client;

internal sealed class ControlMClient(HttpClient httpClient, ControlMClientOptions options) : IControlMClient
{
    public async Task<T> SendAsync<T>(HttpMethod method, string path, object? body = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(method, new Uri(options.Endpoint + path));
        using var response = await httpClient.SendAsync(request, cancellationToken);
        return default!;
    }

    public Task SendAsync(HttpMethod method, string path, object? body = null, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
