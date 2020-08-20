using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion.Internal;

namespace Stl.Fusion
{
    public static partial class ComputedEx
    {
        private static readonly TimeSpan CancelKeepAliveThreshold = TimeSpan.FromSeconds(1.1);

        public static void SetOutput<T>(this IComputed<T> computed, Result<T> output)
        {
            if (!computed.TrySetOutput(output))
                throw Errors.WrongComputedState(ComputedState.Computing, computed.State);
        }

        public static Task WhenInvalidatedAsync<T>(this IComputed<T> computed, CancellationToken cancellationToken = default)
        {
            if (computed.State == ComputedState.Invalidated)
                return Task.CompletedTask;
            var ts = TaskSource.New<Unit>(true);
            computed.Invalidated += c => ts.SetResult(default);
            return ts.Task.WithFakeCancellation(cancellationToken);
        }
    }
}
