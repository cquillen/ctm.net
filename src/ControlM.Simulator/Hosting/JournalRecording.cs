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
        await next(context);
        journal.Record(new JournalEntry(
            context.GetEndpoint()?.Metadata.GetMetadata<Operation>(),
            request.Method,
            request.Path.Value ?? string.Empty,
            QueryWithoutMark(request.QueryString),
            context.Response.StatusCode));
    }

    private static string QueryWithoutMark(QueryString query) =>
        query.HasValue ? query.Value![1..] : string.Empty;
}
