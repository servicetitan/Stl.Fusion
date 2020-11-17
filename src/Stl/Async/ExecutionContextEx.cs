using System.Threading;

namespace Stl.Async
{
    public static class ExecutionContextEx
    {
        public static Disposable<AsyncFlowControl> SuppressFlow()
        {
            if (ExecutionContext.IsFlowSuppressed())
                return Disposable.New<AsyncFlowControl>(default, _ => {});
            var releaser = ExecutionContext.SuppressFlow();
            return Disposable.New(releaser, r => r.Dispose());
        }
    }
}
