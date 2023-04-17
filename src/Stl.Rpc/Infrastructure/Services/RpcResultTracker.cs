namespace Stl.Rpc.Infrastructure;

public class RpcResultTracker
{
    private long _lastResultId;

    public Task<T> Create<T>(out long resultId)
    {
        resultId = Interlocked.Increment(ref _lastResultId);
        return TaskSource.New<T>(true).Task;
    }

    public Task Complete(long resultId, object result)
    {
        return Task.CompletedTask;
    }
}
