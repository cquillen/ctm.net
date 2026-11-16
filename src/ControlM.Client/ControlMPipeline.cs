using Microsoft.Extensions.DependencyInjection;

namespace ControlM.Client;

internal static class ControlMPipeline
{
    // Handlers are added here, outermost first, in the order of docs/requirements.md section 3.4:
    // Retry, Authentication, Capture, then the primary handler.
    public static IHttpClientBuilder Compose(IHttpClientBuilder builder) => builder;
}
