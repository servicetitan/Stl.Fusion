using System;
using System.Reactive;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion.Caching;
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

        public static void KeepAlive(this IComputed computed)
        {
            var options = computed.Options;
            switch (options.IsCachingEnabled) {
            case true:
                var cachingOptions = options.CachingOptions;
                var outputReleaseTime = cachingOptions.OutputReleaseTime;
                if (outputReleaseTime == TimeSpan.MaxValue)
                    goto default;
                if (outputReleaseTime != TimeSpan.MaxValue && computed.State != ComputedState.Invalidated)
                    Timers.DropCachedOutput.AddOrUpdateToLater((ICachingComputed) computed, Timers.Clock.Now + outputReleaseTime);
                break;
            default:
                var keepAliveTime = options.KeepAliveTime;
                if (keepAliveTime != TimeSpan.Zero && computed.State != ComputedState.Invalidated)
                    Timers.KeepAlive.AddOrUpdateToLater(computed, Timers.Clock.Now + keepAliveTime);
                break;
            }
        }

        public static void CancelKeepAlive(this IComputed computed)
        {
            var options = computed.Options;
            switch (options.IsCachingEnabled) {
            case true:
                var cachingOptions = options.CachingOptions;
                var outputReleaseTime = cachingOptions.OutputReleaseTime;
                if (outputReleaseTime == TimeSpan.MaxValue)
                    goto default;
                if (outputReleaseTime != TimeSpan.MaxValue)
                    Timers.DropCachedOutput.Remove((ICachingComputed) computed);
                break;
            default:
                var keepAliveTime = options.KeepAliveTime;
                if (keepAliveTime != TimeSpan.Zero)
                    Timers.KeepAlive.Remove(computed);
                break;
            }
        }
    }
}
