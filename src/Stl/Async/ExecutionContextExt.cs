namespace Stl.Async;

public static class ExecutionContextExt
{
    public static ClosedDisposable<AsyncFlowControl> SuppressFlow()
    {
        if (ExecutionContext.IsFlowSuppressed())
            return default;

        var releaser = ExecutionContext.SuppressFlow();
        return Disposable.NewClosed(releaser, r => r.Dispose());
    }
}
