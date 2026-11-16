using ControlM.Client.Operations;
using ControlM.Simulator.Journal;
using Microsoft.AspNetCore.Http;

namespace ControlM.Simulator.Hosting;

// Runs after routing, to read the matched endpoint's operation, and around every endpoint and endpoint filter, to see
// the status code each one answers.
internal sealed class JournalRecording(RequestJournal journal, TimeProvider time)
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var request = context.Request;
        if (request.Path.StartsWithSegments(ControlApiPath.Base))
        {
            await next(context);
            return;
        }

        var slot = journal.Admit();
        var arrival = time.GetUtcNow();
        await next(context);
        journal.Record(slot, new JournalEntry(
            context.GetEndpoint()?.Metadata.GetMetadata<Operation>(),
            request.Method,
            request.Path.Value ?? string.Empty,
            QueryWithoutMark(request.QueryString),
            arrival,
            context.Response.StatusCode));
    }

    private static string QueryWithoutMark(QueryString query) =>
        query.HasValue ? query.Value![1..] : string.Empty;
}
