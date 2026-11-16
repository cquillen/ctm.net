namespace ControlM.Client.Tests;

// Without a stub it never answers. Over a stub it passes requests through, except that from Hold onward
// the requests to the held path wait until Release.
internal sealed class HangingHandler : DelegatingHandler
{
    private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private string? _heldPath;

    public HangingHandler()
    {
    }

    public HangingHandler(StubHandler stub)
        : base(stub)
    {
    }

    public Task Entered => _entered.Task;

    public void Hold(string absolutePath) => _heldPath = absolutePath;

    public void Release() => _released.SetResult();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (InnerHandler is not null && request.RequestUri!.AbsolutePath != _heldPath)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        _entered.TrySetResult();
        await _released.Task.WaitAsync(cancellationToken);
        return await base.SendAsync(request, cancellationToken);
    }
}
