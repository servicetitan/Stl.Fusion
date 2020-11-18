using System.Threading;

namespace Stl.Async
{
    public static class ExecutionContextEx
    {
        public static ClosedDisposable<AsyncFlowControl> SuppressFlow()
        {
            if (ExecutionContext.IsFlowSuppressed())
                return Disposable.NewClosed<AsyncFlowControl>(default, _ => {});
            var releaser = ExecutionContext.SuppressFlow();
            return Disposable.NewClosed(releaser, r => r.Dispose());
        }
    }
}
