using System.Net;
using System.Text;

namespace ControlM.Client.Tests;

internal sealed record RecordedRequest(HttpMethod Method, Uri Uri, string? Body);

internal sealed class StubHandler(HttpStatusCode status, string responseBody) : HttpMessageHandler
{
    public List<RecordedRequest> Requests { get; } = [];

    public static StubHandler Returning(HttpStatusCode status, string responseBody) => new(status, responseBody);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add(new RecordedRequest(request.Method, request.RequestUri!, body));
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
        };
    }
}
