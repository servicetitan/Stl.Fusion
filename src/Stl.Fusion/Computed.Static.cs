using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion.Internal;

namespace Stl.Fusion
{
    public static partial class Computed
    {
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

        public static Disposable<IComputed?> Suppress() => ChangeCurrent(null);

        // TryCapture & Capture

        public static async Task<IComputed<T>?> TryCaptureAsync<T>(Func<CancellationToken, Task<T>> producer, CancellationToken cancellationToken = default)
        {
            using var ccs = ComputeContext.New(CallOptions.Capture).Activate();
            IComputed<T>? result;
            try {
                await producer.Invoke(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                result = ccs.Context.GetCapturedComputed<T>();
                if (result?.Error != null)
                    return result;
                throw;
            }
            result = ccs.Context.GetCapturedComputed<T>();
            return result;
        }

        public static async Task<IComputed<T>?> TryCaptureAsync<T>(Func<CancellationToken, ValueTask<T>> producer, CancellationToken cancellationToken = default)
        {
            using var ccs = ComputeContext.New(CallOptions.Capture).Activate();
            IComputed<T>? result;
            try {
                await producer.Invoke(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                result = ccs.Context.GetCapturedComputed<T>();
                if (result?.Error != null)
                    return result;
                throw;
            }
            result = ccs.Context.GetCapturedComputed<T>();
            return result;
        }

        public static async Task<IComputed<T>> CaptureAsync<T>(Func<CancellationToken, Task<T>> producer, CancellationToken cancellationToken = default)
        {
            using var ccs = ComputeContext.New(CallOptions.Capture).Activate();
            IComputed<T>? result;
            try {
                await producer.Invoke(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                result = ccs.Context.GetCapturedComputed<T>();
                if (result == null)
                    throw Errors.NoComputedCaptured();
                if (result?.Error != null)
                    return result;
                throw;
            }
            result = ccs.Context.GetCapturedComputed<T>();
            if (result == null)
                throw Errors.NoComputedCaptured();
            return result;
        }

        public static async Task<IComputed<T>> CaptureAsync<T>(Func<CancellationToken, ValueTask<T>> producer, CancellationToken cancellationToken = default)
        {
            using var ccs = ComputeContext.New(CallOptions.Capture).Activate();
            IComputed<T>? result;
            try {
                await producer.Invoke(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                result = ccs.Context.GetCapturedComputed<T>();
                if (result == null)
                    throw Errors.NoComputedCaptured();
                if (result?.Error != null)
                    return result;
                throw;
            }
            result = ccs.Context.GetCapturedComputed<T>();
            if (result == null)
                throw Errors.NoComputedCaptured();
            return result;
        }

        // Invalidate

        public static IComputed<T>? Invalidate<T>(Func<Task<T>> producer)
        {
            using var ccs = ComputeContext.New(CallOptions.Invalidate).Activate();
            var task = producer.Invoke();
            // The flow is essentially synchronous in this case, so...
            task.AssertCompleted();
            return ccs.Context.GetCapturedComputed<T>();
        }

        public static IComputed<T>? Invalidate<T>(Func<ValueTask<T>> producer)
        {
            using var ccs = ComputeContext.New(CallOptions.Invalidate).Activate();
            var task = producer.Invoke();
            // The flow is essentially synchronous in this case, so...
            task.AssertCompleted();
            return ccs.Context.GetCapturedComputed<T>();
        }

        // TryGetExisting

        public static IComputed<T>? TryGetExisting<T>(Func<Task<T>> producer)
        {
            using var ccs = ComputeContext.New(CallOptions.TryGetExisting).Activate();
            var task = producer.Invoke();
            // The flow is essentially synchronous in this case, so...
            task.AssertCompleted();
            return ccs.Context.GetCapturedComputed<T>();
        }

        public static IComputed<T>? TryGetExisting<T>(Func<ValueTask<T>> producer)
        {
            using var ccs = ComputeContext.New(CallOptions.TryGetExisting).Activate();
            var task = producer.Invoke();
            // The flow is essentially synchronous in this case, so...
            task.AssertCompleted();
            return ccs.Context.GetCapturedComputed<T>();
        }
    }
}
