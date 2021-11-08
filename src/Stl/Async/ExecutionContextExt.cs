namespace Stl.Async;

public static class ExecutionContextExt
{
    public static ClosedDisposable<AsyncFlowControl> SuppressFlow()
    {
        if (ExecutionContext.IsFlowSuppressed())
            return Disposable.NewClosed<AsyncFlowControl>(default, _ => {});
        var releaser = ExecutionContext.SuppressFlow();
        return Disposable.NewClosed(releaser, r => r.Dispose());
    }
}
