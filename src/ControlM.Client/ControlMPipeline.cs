using Microsoft.Extensions.DependencyInjection;

namespace ControlM.Client;

internal static class ControlMPipeline
{
    // The one place the handlers of docs/requirements.md section 3.4 are composed.
    public static IHttpClientBuilder Compose(IHttpClientBuilder builder) => builder;
}
