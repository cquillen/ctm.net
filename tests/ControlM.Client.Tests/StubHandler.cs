using System.Net;
using System.Text;

namespace ControlM.Client.Tests;

internal sealed record RecordedRequest(
    HttpMethod Method,
    Uri Uri,
    string? Body,
    IReadOnlyDictionary<string, IEnumerable<string>> Headers);

internal sealed class StubHandler(HttpStatusCode status, string responseBody) : HttpMessageHandler
{
    private readonly Dictionary<string, (HttpStatusCode Status, string Body)> _byPath = new(StringComparer.Ordinal);
    private readonly HashSet<string> _failingPaths = new(StringComparer.Ordinal);

    public List<RecordedRequest> Requests { get; } = [];

    public static StubHandler Returning(HttpStatusCode status, string responseBody) => new(status, responseBody);

    public StubHandler Answering(string absolutePath, HttpStatusCode pathStatus, string pathBody)
    {
        _byPath[absolutePath] = (pathStatus, pathBody);
        return this;
    }

    public StubHandler Failing(string absolutePath)
    {
        _failingPaths.Add(absolutePath);
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        var headers = request.Headers.ToDictionary(h => h.Key, h => h.Value, StringComparer.OrdinalIgnoreCase);
        Requests.Add(new RecordedRequest(request.Method, request.RequestUri!, body, headers));
        if (_failingPaths.Contains(request.RequestUri!.AbsolutePath))
        {
            throw new HttpRequestException("The stub refused the connection.");
        }

        var (answerStatus, answerBody) = _byPath.GetValueOrDefault(request.RequestUri!.AbsolutePath, (status, responseBody));
        return new HttpResponseMessage(answerStatus)
        {
            Content = new StringContent(answerBody, Encoding.UTF8, "application/json"),
        };
    }
}
