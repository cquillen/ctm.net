using System.Net;
using System.Text.Json;
using ControlM.Client.Errors;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ControlM.Client.Tests.Authentication.ApiKey;

public sealed class ApiKeyTests
{
    [Test]
    public async Task The_configured_key_is_sent_in_the_x_api_key_header()
    {
        var stub = StubHandler.Returning(HttpStatusCode.OK, "[]");
        var client = TestClient.Create(stub);

        await client.SendAsync<JsonElement>(HttpMethod.Get, "/config/servers");

        stub.Requests.ShouldHaveSingleItem().Headers.GetValueOrDefault("x-api-key").ShouldBe([TestClient.ApiKey]);
    }

    [Test]
    public async Task A_request_sent_twice_through_the_pipeline_carries_the_key_once()
    {
        var stub = StubHandler.Returning(HttpStatusCode.OK, "[]");
        var services = new ServiceCollection();
        services.AddControlMClient(o =>
            {
                o.Endpoint = new Uri(TestClient.Endpoint);
                o.UseApiKey(TestClient.ApiKey);
            })
            .ConfigurePrimaryHttpMessageHandler(() => stub)
            .ConfigureAdditionalHttpMessageHandlers((handlers, _) => handlers.Insert(0, new ResendingHandler()));
        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IControlMClient>();

        await client.SendAsync<JsonElement>(HttpMethod.Get, "/config/servers");

        stub.Requests[^1].Headers.GetValueOrDefault("x-api-key").ShouldBe([TestClient.ApiKey]);
    }

    [Test]
    public async Task A_call_answered_401_is_not_sent_again()
    {
        var stub = StubHandler.Returning(HttpStatusCode.Unauthorized, """{"errors":[{"message":"Unauthenticated request"}]}""");
        var client = TestClient.Create(stub);

        await Should.ThrowAsync<ControlMAuthenticationException>(() => client.SendAsync(HttpMethod.Get, "/config/servers"));

        stub.Requests.ShouldHaveSingleItem();
    }

    private sealed class ResendingHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            (await base.SendAsync(request, cancellationToken)).Dispose();
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
