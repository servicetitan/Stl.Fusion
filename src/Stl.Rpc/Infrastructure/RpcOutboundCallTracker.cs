namespace Stl.Rpc.Infrastructure;

public class RpcOutboundCallTracker
{
    private long _lastResultId;

    public Task<T> Create<T>(out long resultId)
    {
        resultId = Interlocked.Increment(ref _lastResultId);
        return Task.FromResult(default(T)!);
    }

    public Task Complete(long resultId, object result)
    {
        return Task.CompletedTask;
    }

    public void Register(RpcBoundRequest boundRequest)
    {
        var methodDef = boundRequest.MethodDef;
        var cancellationTokenIndex = methodDef.CancellationTokenIndex;
        var cancellationToken = cancellationTokenIndex >= 0
            ? boundRequest.Arguments.GetCancellationToken(cancellationTokenIndex)
            : default;
    }
}
