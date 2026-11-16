using Microsoft.Extensions.DependencyInjection;

namespace ControlM.Client;

internal static class ControlMPipeline
{
    // Handlers are added here, outermost first: Retry, Authentication, Capture, then the primary handler.
    // Capture sits inside Retry and Authentication so that it records each attempt as it was sent.
    public static IHttpClientBuilder Compose(IHttpClientBuilder builder) => builder;
}
