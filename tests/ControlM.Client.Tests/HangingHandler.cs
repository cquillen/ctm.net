namespace ControlM.Client.Tests;

// Without a stub it never answers. Over a stub it passes requests through, except that from Hold onward
// the requests to the held path wait until Release. CountAnswers completes once the stub has answered that many
// requests to a path.
internal sealed class HangingHandler : DelegatingHandler
{
    private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _answered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private string? _heldPath;
    private string? _countedPath;
    private int _answersToCount;

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

    public Task CountAnswers(string absolutePath, int count)
    {
        _answersToCount = count;
        _countedPath = absolutePath;
        return _answered.Task;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath;
        if (InnerHandler is null || path == _heldPath)
        {
            _entered.TrySetResult();
            await _released.Task.WaitAsync(cancellationToken);
        }

        var response = await base.SendAsync(request, cancellationToken);
        if (path == _countedPath && Interlocked.Decrement(ref _answersToCount) == 0)
        {
            _answered.TrySetResult();
        }

        return response;
    }
}
