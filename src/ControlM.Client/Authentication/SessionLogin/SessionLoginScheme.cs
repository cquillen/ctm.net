using System.Net.Http.Headers;
using System.Net.Http.Json;
using ControlM.Client.Authentication.Login;
using ControlM.Client.Errors;
using ControlM.Client.Operations;
using ControlM.Client.Serialization;

namespace ControlM.Client.Authentication.SessionLogin;

// Login and logout go through the client's own named HttpClient, so they reach the primary handler the consumer
// configured; the request option keeps the authentication handler from applying a credential to them.
internal sealed class SessionLoginScheme(
    LoginCredentials credentials,
    Uri endpoint,
    IHttpClientFactory httpClientFactory,
    string httpClientName) : ICredentialScheme, IAsyncDisposable
{
    public const string AuthenticationScheme = "Bearer";

    private readonly SemaphoreSlim _login = new(1, 1);

    private volatile string? _token;

    public async ValueTask ApplyAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _token ?? await ReplaceTokenAsync(refusedToken: null, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue(AuthenticationScheme, token);
    }

    public async ValueTask<bool> TryRecoverAsync(HttpRequestMessage refused, CancellationToken cancellationToken)
    {
        await ReplaceTokenAsync(refused.Headers.Authorization?.Parameter, cancellationToken);
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        _login.Dispose();
        if (_token is null)
        {
            return;
        }

        try
        {
            await LogoutAsync(_token);
        }
        catch (Exception)
        {
            // Disposal never throws (docs/requirements.md section 3.4): a session the server keeps is its to expire.
        }
    }

    // Callers that wait here while another logs in find the token replaced, and so share that login.
    private async Task<string> ReplaceTokenAsync(string? refusedToken, CancellationToken cancellationToken)
    {
        await _login.WaitAsync(cancellationToken);
        try
        {
            if (_token is { } current && current != refusedToken)
            {
                return current;
            }

            return _token = await LoginAsync(cancellationToken);
        }
        finally
        {
            _login.Release();
        }
    }

    private async Task LogoutAsync(string token)
    {
        using var request = SessionRequest(Operation.Logout);
        request.Headers.Authorization = new AuthenticationHeaderValue(AuthenticationScheme, token);
        using var http = httpClientFactory.CreateClient(httpClientName);
        using var response = await http.SendAsync(request, CancellationToken.None);
    }

    private async Task<string> LoginAsync(CancellationToken cancellationToken)
    {
        var login = Operation.Login;
        using var request = SessionRequest(login);
        request.Content = JsonContent.Create(credentials, options: ControlMJson.Options);
        using var http = httpClientFactory.CreateClient(httpClientName);
        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw await ApiFailure.ReadAsync(response, login.Method, login.Path, login, cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<LoginResult>(ControlMJson.Options, cancellationToken);
        return result!.Token;
    }

    private HttpRequestMessage SessionRequest(Operation operation)
    {
        var request = new HttpRequestMessage(operation.Method, RequestUri.Of(endpoint, operation.Path));
        request.Options.Set(AuthenticationHandler.Unauthenticated, true);
        return request;
    }
}
