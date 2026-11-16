using System.Net.Http.Headers;
using System.Net.Http.Json;
using ControlM.Client.Authentication.Login;
using ControlM.Client.Errors;
using ControlM.Client.Operations;
using ControlM.Client.Serialization;

namespace ControlM.Client.Authentication.SessionLogin;

// Login goes through the client's own named HttpClient, so it reaches the primary handler the consumer configured;
// the request option keeps the authentication handler from applying a credential to it.
internal sealed class SessionLoginScheme(
    LoginCredentials credentials,
    Uri endpoint,
    IHttpClientFactory httpClientFactory,
    string httpClientName) : ICredentialScheme
{
    public const string AuthenticationScheme = "Bearer";

    private string? _token;

    public async ValueTask ApplyAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _token ??= await LoginAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue(AuthenticationScheme, _token);
    }

    private async Task<string> LoginAsync(CancellationToken cancellationToken)
    {
        var login = Operation.Login;
        using var request = new HttpRequestMessage(login.Method, RequestUri.Of(endpoint, login.Path))
        {
            Content = JsonContent.Create(credentials, options: ControlMJson.Options),
        };
        request.Options.Set(AuthenticationHandler.Unauthenticated, true);
        using var http = httpClientFactory.CreateClient(httpClientName);
        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw await ApiFailure.ReadAsync(response, login.Method, login.Path, login, cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<LoginResult>(ControlMJson.Options, cancellationToken);
        return result!.Token;
    }
}
