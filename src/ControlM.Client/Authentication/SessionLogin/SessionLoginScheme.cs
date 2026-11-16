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
    string httpClientName) : ICredentialScheme, IAsyncDisposable, IDisposable
{
    public const string AuthenticationScheme = "Bearer";

    private readonly SemaphoreSlim _login = new(1, 1);

    private volatile string? _token;

    private int _disposed;

    public async ValueTask ApplyAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _token ?? await ObtainTokenAsync(refusedToken: null, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue(AuthenticationScheme, token);
    }

    public async ValueTask<bool> TryRecoverAsync(HttpRequestMessage refused, CancellationToken cancellationToken)
    {
        await ObtainTokenAsync(refused.Headers.Authorization?.Parameter, cancellationToken);
        return true;
    }

    // Taking the lock lets a login still in flight finish, so its session is the one logged out.
    public async ValueTask DisposeAsync()
    {
        if (!TryBeginDisposal())
        {
            return;
        }

        try
        {
            await _login.WaitAsync();
            if (_token is { } token)
            {
                await LogoutAsync(token);
            }
        }
        catch (Exception)
        {
            // Disposal never throws (docs/requirements.md section 3.4): a session the server keeps is its to expire.
        }
        finally
        {
            _login.Dispose();
        }
    }

    // A synchronous disposal cannot log out without blocking, so it leaves the session for the server to expire
    // (docs/requirements.md section 3.4). A login still in flight holds the lock and releases it later, so the lock is
    // then left undisposed rather than make that release throw; its wait handle is never created, so nothing leaks.
    public void Dispose()
    {
        if (!TryBeginDisposal() || !_login.Wait(0))
        {
            return;
        }

        _login.Dispose();
    }

    private bool TryBeginDisposal() => Interlocked.Exchange(ref _disposed, 1) == 0;

    // Callers that wait here while another logs in find that login's token, and so share it.
    private async Task<string> ObtainTokenAsync(string? refusedToken, CancellationToken cancellationToken)
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
