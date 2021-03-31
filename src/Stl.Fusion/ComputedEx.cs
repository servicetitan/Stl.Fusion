using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion.Internal;
using Stl.Internal;
using Errors = Stl.Fusion.Internal.Errors;

namespace Stl.Fusion
{
    public static class ComputedEx
    {
        private static readonly ObjectHolder ObjectHolder = new();

        public static void Invalidate(this IComputed computed, TimeSpan delay, bool? usePreciseTimer = null)
        {
            if (delay <= TimeSpan.Zero) {
                computed.Invalidate();
                return;
            }

            var bPrecise = usePreciseTimer ?? delay <= TimeSpan.FromSeconds(1);
            if (!bPrecise) {
                Timeouts.Invalidate.AddOrUpdateToEarlier(computed, Timeouts.Clock.Now + delay);
                computed.Invalidated += c => Timeouts.Invalidate.Remove(c);
                return;
            }

            var cts = new CancellationTokenSource(delay);
            computed.Invalidated += _ => {
                try {
                    if (!cts.IsCancellationRequested)
                        cts.Cancel(true);
                } catch {
                    // Intended: this method should never throw any exceptions
                }
            };
            cts.Token.Register(() => {
                // No need to schedule this via Task.Run, since this code is
                // either invoked from Invalidate method (via Invalidated handler),
                // so Invalidate() call will do nothing & return immediately,
                // or it's invoked via one of timer threads, i.e. where it's
                // totally fine to invoke Invalidate directly as well.
                computed.Invalidate();
                cts.Dispose();
            });
        }

        public static async Task WhenInvalidated(this IComputed computed, CancellationToken cancellationToken = default)
        {
            if (computed.ConsistencyState == ConsistencyState.Invalidated)
                return;
            // Why holding? If the calling task isn't referenced from alive set,
            // (e.g. it is simply started w/ Run), there is nothing that may
            // prevent GC from collecting it + computed, even though WhenInvalidatedAsync
            // implies waiting, right?
            var ts = TaskSource.New<Unit>(true);
            var onInvalidated = (Action<IComputed>) (_ => ts.SetResult(default));
            computed.Invalidated += onInvalidated;
            var holder = ObjectHolder.Hold(computed);
            try {
                await ts.Task.WithFakeCancellation(cancellationToken).ConfigureAwait(false);
                // No need to remove onInvalidated handler in case of success:
                // all handlers are auto-removed on invalidation.
            }
            catch {
                computed.Invalidated -= onInvalidated;
                throw;
            }
            finally {
                holder.Dispose();
            }
        }

        public static void SetOutput<T>(this IComputed<T> computed, Result<T> output)
        {
            if (!computed.TrySetOutput(output))
                throw Errors.WrongComputedState(ConsistencyState.Computing, computed.ConsistencyState);
        }
        
        #if NETSTANDARD2_0

        public static IComputed CompleteCapture(this ComputeContextScope ccs, bool wasError)
        {
            try {
                var result = ccs.Context.GetCapturedComputed();

                if (wasError) {
                    if (result?.Error!=null)
                        return result;
                }

                if (result==null)
                    throw Errors.NoComputedCaptured();
                return result;
            }
            finally {
                ccs.Dispose();
            }
        }

        #endif
    }
}
