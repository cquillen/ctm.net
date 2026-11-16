namespace ControlM.Client;

internal sealed class ControlMClient(HttpClient httpClient, ControlMClientOptions options) : IControlMClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ControlMClientOptions _options = options;

    public Task<T> SendAsync<T>(HttpMethod method, string path, object? body = null, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task SendAsync(HttpMethod method, string path, object? body = null, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
