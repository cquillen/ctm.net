using System.Text;
using ControlM.Client.Operations;
using ControlM.Simulator.Journal;
using Microsoft.AspNetCore.Http;

namespace ControlM.Simulator.Hosting;

// Runs after routing, to read the matched endpoint's operation, and around every endpoint and endpoint filter, to see
// the status code each one answers.
internal sealed class JournalRecording(RequestJournal journal)
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var request = context.Request;
        if (request.Path.StartsWithSegments(ControlApiPath.Base))
        {
            await next(context);
            return;
        }

        var (slot, arrival) = journal.Admit();
        var body = string.Empty;
        var answered = false;
        try
        {
            body = await ReadBodyAsync(request, context.RequestAborted);
            await next(context);
            answered = true;
        }
        finally
        {
            journal.Record(slot, new JournalEntry(
                context.GetEndpoint()?.Metadata.GetMetadata<Operation>(),
                request.Method,
                request.Path.Value ?? string.Empty,
                QueryWithoutMark(request.QueryString),
                HeadersAsReceived(request.Headers),
                body,
                arrival,
                StatusSent(context.Response, answered)));
        }
    }

    // An exception that escapes before the response starts leaves the answer, if any, to the host, not the simulator.
    private static int? StatusSent(HttpResponse response, bool answered) =>
        answered || response.HasStarted ? response.StatusCode : null;

    // The body is buffered and rewound so the endpoint still reads all of it.
    private static async Task<string> ReadBodyAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var body = await reader.ReadToEndAsync(cancellationToken);
        request.Body.Position = 0;
        return body;
    }

    private static Dictionary<string, string> HeadersAsReceived(IHeaderDictionary headers) =>
        headers.ToDictionary(
            header => header.Key,
            header => string.Join(", ", header.Value.AsEnumerable()),
            StringComparer.OrdinalIgnoreCase);

    private static string QueryWithoutMark(QueryString query) =>
        query.HasValue ? query.Value![1..] : string.Empty;
}
