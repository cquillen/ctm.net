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
    private readonly Lock _gate = new();
    private readonly Dictionary<string, Queue<(HttpStatusCode Status, string Body)>> _byPath = new(StringComparer.Ordinal);
    private readonly HashSet<string> _failingPaths = new(StringComparer.Ordinal);

    public List<RecordedRequest> Requests { get; } = [];

    public static StubHandler Returning(HttpStatusCode status, string responseBody) => new(status, responseBody);

    public StubHandler Answering(string absolutePath, HttpStatusCode pathStatus, string pathBody) =>
        AnsweringInTurn(absolutePath, (pathStatus, pathBody));

    // Each request to the path takes the next answer; the last one answers every request after it.
    public StubHandler AnsweringInTurn(string absolutePath, params (HttpStatusCode Status, string Body)[] answers)
    {
        _byPath[absolutePath] = new Queue<(HttpStatusCode, string)>(answers);
        return this;
    }

    public StubHandler Failing(string absolutePath)
    {
        _failingPaths.Add(absolutePath);
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = await BodyOf(request, cancellationToken);
        var headers = request.Headers.ToDictionary(h => h.Key, h => h.Value, StringComparer.OrdinalIgnoreCase);
        var (answerStatus, answerBody) = Record(new RecordedRequest(request.Method, request.RequestUri!, body, headers));
        if (_failingPaths.Contains(request.RequestUri!.AbsolutePath))
        {
            throw new HttpRequestException("The stub refused the connection.");
        }

        return new HttpResponseMessage(answerStatus)
        {
            Content = new StringContent(answerBody, Encoding.UTF8, "application/json"),
        };
    }

    // Copied out rather than read as a string, so the stub serializes the content as a network handler does
    // and does not buffer it on the caller's behalf.
    private static async Task<string?> BodyOf(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content is null)
        {
            return null;
        }

        using var copy = new MemoryStream();
        await request.Content.CopyToAsync(copy, cancellationToken);
        return Encoding.UTF8.GetString(copy.ToArray());
    }

    private (HttpStatusCode Status, string Body) Record(RecordedRequest request)
    {
        lock (_gate)
        {
            Requests.Add(request);
            if (!_byPath.TryGetValue(request.Uri.AbsolutePath, out var answers))
            {
                return (status, responseBody);
            }

            return answers.Count > 1 ? answers.Dequeue() : answers.Peek();
        }
    }
}
