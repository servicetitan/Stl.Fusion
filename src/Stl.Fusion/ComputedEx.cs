using System;
using System.Reactive;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion.Internal;

namespace Stl.Fusion
{
    public static partial class ComputedEx
    {
        private static readonly TimeSpan CancelKeepAliveThreshold = TimeSpan.FromSeconds(1.1);

        public static Task WhenInvalidatedAsync<T>(this IComputed<T> computed, CancellationToken cancellationToken = default)
        {
            if (computed.State == ComputedState.Invalidated)
                return Task.CompletedTask;
            var ts = TaskSource.New<Unit>(true);
            computed.Invalidated += c => ts.SetResult(default);
            return ts.Task.WithFakeCancellation(cancellationToken);
        }

        public static void KeepAlive(this IComputed computed)
        {
            var keepAliveTime = computed.Options.KeepAliveTime;
            if (keepAliveTime > TimeSpan.Zero && computed.State != ComputedState.Invalidated)
                RefHolder.Hold(computed, keepAliveTime);
        }

        public static void CancelKeepAlive(this IComputed computed, TimeSpan threshold)
        {
            var keepAliveTime = computed.Options.KeepAliveTime;
            if (keepAliveTime >= threshold)
                RefHolder.Release(computed);
        }

        public static void CancelKeepAlive(this IComputed computed)
        {
            var keepAliveTime = computed.Options.KeepAliveTime;
            if (keepAliveTime >= CancelKeepAliveThreshold)
                RefHolder.Release(computed);
        }
    }
}
