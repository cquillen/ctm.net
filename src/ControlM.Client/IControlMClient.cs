namespace ControlM.Client;

/// <summary>
/// A client for the Control-M Automation API. Register it with
/// <see cref="ControlMClientServiceCollectionExtensions.AddControlMClient"/>.
/// </summary>
public interface IControlMClient
{
    /// <summary>
    /// Sends a request to any operation, including one the typed members do not cover, and returns the
    /// successful response body deserialized as <typeparamref name="T"/>. Properties of the response that
    /// <typeparamref name="T"/> does not declare are ignored.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">
    /// The path relative to the endpoint, such as <c>/run/jobs/status?folder=FOLDER1</c>. It may carry a query string.
    /// </param>
    /// <param name="body">An object sent as the JSON body, or <see langword="null"/> to send no body.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    Task<T> SendAsync<T>(HttpMethod method, string path, object? body = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a request to any operation and completes when it succeeds, without reading the response body.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">
    /// The path relative to the endpoint, such as <c>/run/job/SERVER1:00001/hold</c>. It may carry a query string.
    /// </param>
    /// <param name="body">An object sent as the JSON body, or <see langword="null"/> to send no body.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    Task SendAsync(HttpMethod method, string path, object? body = null, CancellationToken cancellationToken = default);
}
