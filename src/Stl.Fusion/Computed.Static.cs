using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion.Internal;
using Stl.Fusion.Bridge;
using Stl.Time;

namespace Stl.Fusion
{
    public static class Computed
    {
        public static readonly int DefaultKeepAliveTime = IntMoment.SecondsToUnits(1);
        private static readonly AsyncLocal<IComputed?> CurrentLocal = new AsyncLocal<IComputed?>();

        // GetCurrent & ChangeCurrent

        public static IComputed? GetCurrent() => CurrentLocal.Value;

        public static IComputed<T> GetCurrent<T>()
        {
            var untypedCurrent = GetCurrent();
            if (untypedCurrent is IComputed<T> c)
                return c;
            if (untypedCurrent == null)
                throw Errors.ComputedCurrentIsNull();
            throw Errors.ComputedCurrentIsOfIncompatibleType(typeof(IComputed<T>));
        }

        public static Disposable<IComputed?> ChangeCurrent(IComputed? newCurrent)
        {
            var oldCurrent = GetCurrent();
            if (oldCurrent == newCurrent)
                return Disposable.New(oldCurrent, _ => { });
            CurrentLocal.Value = newCurrent;
            return Disposable.New(oldCurrent, oldCurrent1 => CurrentLocal.Value = oldCurrent1);
        }

        // Capture & invalidate

        public static async Task<IComputed<T>?> TryCaptureAsync<T>(Func<CancellationToken, Task<T>> producer, CancellationToken cancellationToken = default)
        {
            using var ccs = ComputeContext.New(ComputeOptions.Capture).Activate();
            await producer.Invoke(cancellationToken).ConfigureAwait(false);
            var result = ccs.Context.GetCapturedComputed<T>();
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<IComputed<T>> CaptureAsync<T>(Func<CancellationToken, Task<T>> producer, CancellationToken cancellationToken = default)
        {
            using var ccs = ComputeContext.New(ComputeOptions.Capture).Activate();
            await producer.Invoke(cancellationToken).ConfigureAwait(false);
            var result = ccs.Context.GetCapturedComputed<T>();
            if (result == null)
                throw Errors.NoComputedCaptured();
            return result;
        }

        public static IComputed<T>? Invalidate<T>(Func<Task<T>> producer, object? invalidatedBy = null)
        {
            using var ccs = ComputeContext.New(ComputeOptions.Invalidate, invalidatedBy).Activate();
            var task = producer.Invoke();
            // The flow is essentially synchronous in this case, so...
            task.AssertCompleted();
            return ccs.Context.GetCapturedComputed<T>();
        }

        public static IComputed<T>? Invalidate<T>(Func<CancellationToken, Task<T>> producer, object? invalidatedBy = null, CancellationToken cancellationToken = default)
        {
            using var ccs = ComputeContext.New(ComputeOptions.Invalidate, invalidatedBy).Activate();
            var task = producer.Invoke(cancellationToken);
            // The flow is essentially synchronous in this case, so...
            task.AssertCompleted();
            return ccs.Context.GetCapturedComputed<T>();
        }

        public static IComputed<T>? TryGetCached<T>(Func<Task<T>> producer)
        {
            using var ccs = ComputeContext.New(ComputeOptions.TryGetCached).Activate();
            var task = producer.Invoke();
            // The flow is essentially synchronous in this case, so...
            task.AssertCompleted();
            return ccs.Context.GetCapturedComputed<T>();
        }

        public static IComputed<T>? TryGetCached<T>(Func<CancellationToken, Task<T>> producer, CancellationToken cancellationToken = default)
        {
            using var ccs = ComputeContext.New(ComputeOptions.TryGetCached).Activate();
            var task = producer.Invoke(cancellationToken);
            // The flow is essentially synchronous in this case, so...
            task.AssertCompleted();
            return ccs.Context.GetCapturedComputed<T>();
        }
    }
}
