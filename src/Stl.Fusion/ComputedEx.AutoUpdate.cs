using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion.Internal;
using Stl.Time;

namespace Stl.Fusion
{
    public static partial class ComputedEx
    {
        internal sealed class AutoUpdateApplyHandler : IComputedApplyHandler<
            (TimeSpan, IMomentClock, Delegate?, Delegate?),
            Disposable<(IComputed, CancellationTokenSource, Delegate?)>>
        {
            public static readonly AutoUpdateApplyHandler Instance = new AutoUpdateApplyHandler();

            public Disposable<(IComputed, CancellationTokenSource, Delegate?)> Apply<TIn, TOut>(
                IComputed<TIn, TOut> computed,
                (TimeSpan, IMomentClock, Delegate?, Delegate?) arg)
                where TIn : ComputedInput
            {
                var (delay, clock, untypedHandler, untypedCompletedHandler) = arg;
                var handler = (Action<IComputed<TOut>, Result<TOut>, Exception?>?) untypedHandler;
                var stopCts = new CancellationTokenSource();
                var stopToken = stopCts.Token;

                Task.Run(async () => {
                    var prevComputed = (IComputed<TOut>) computed;
                    while (true) {
                        await prevComputed.WhenInvalidatedAsync(stopToken).ConfigureAwait(false);
                        if (delay > TimeSpan.Zero)
                            await clock!.DelayAsync(delay, stopToken).ConfigureAwait(false);
                        else
                            stopToken.ThrowIfCancellationRequested();
                        IComputed<TOut> nextComputed;
                        Exception? error = null;
                        try {
                            nextComputed = await prevComputed.UpdateAsync(false, stopToken).ConfigureAwait(false);
                        }
                        catch (Exception e) {
                            nextComputed = prevComputed;
                            error = e;
                        }
                        handler?.Invoke(nextComputed, prevComputed.Output, error);
                        prevComputed = nextComputed;
                    }
                }, stopToken);

                return Disposable.New(
                    (Computed: (IComputed) computed, StopCts: stopCts, CompletedHandler: untypedCompletedHandler),
                    state => {
                        try {
                            state.StopCts.Cancel();
                        }
                        finally {
                            state.StopCts.Dispose();
                            if (untypedCompletedHandler is Action<IComputed<TOut>> completedHandler)
                                completedHandler.Invoke(computed);
                        }
                    });
            }
        }

        public static Disposable<(IComputed, CancellationTokenSource, Delegate?)> AutoUpdate<T>(
            this IComputed<T> computed,
            Action<IComputed<T>, Result<T>, Exception?>? recomputed = null,
            Action<IComputed<T>>? completed = null)
            => computed.AutoUpdate(default, null, recomputed, completed);

        public static Disposable<(IComputed, CancellationTokenSource, Delegate?)> AutoUpdate<T>(
            this IComputed<T> computed,
            TimeSpan delay = default,
            Action<IComputed<T>, Result<T>, Exception?>? recomputed = null,
            Action<IComputed<T>>? completed = null)
            => computed.AutoUpdate(delay, null, recomputed, completed);

        public static Disposable<(IComputed, CancellationTokenSource, Delegate?)> AutoUpdate<T>(
            this IComputed<T> computed,
            TimeSpan delay = default,
            IMomentClock? clock = null,
            Action<IComputed<T>, Result<T>, Exception?>? recomputed = null,
            Action<IComputed<T>>? completed = null)
        {
            clock ??= SystemClock.Instance;
            return computed.Apply(
                AutoUpdateApplyHandler.Instance,
                (delay, clock, recomputed, completed));
        }
    }
}
