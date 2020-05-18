using System;
using System.Threading;
using Stl.Fusion.Internal;
using Stl.Time;

namespace Stl.Fusion
{
    public static partial class ComputedEx
    {
        internal sealed class AutoUpdateApplyHandler : IComputedApplyHandler<
            (TimeSpan, IClock, Delegate?, Delegate?), 
            Disposable<(IComputed, CancellationTokenSource, Delegate?)>>
        {
            public static readonly AutoUpdateApplyHandler Instance = new AutoUpdateApplyHandler();
            
            public Disposable<(IComputed, CancellationTokenSource, Delegate?)> Apply<TIn, TOut>(
                IComputed<TIn, TOut> computed, 
                (TimeSpan, IClock, Delegate?, Delegate?) arg) 
                where TIn : ComputedInput
            {
                var (delay, clock, untypedHandler, untypedCompletedHandler) = arg;
                var handler = (Action<IComputed<TOut>, Result<TOut>, object?>?) untypedHandler;
                var stop = new CancellationTokenSource();
                var stopToken = stop.Token;

                async void OnInvalidated(IComputed c, object? invalidatedBy) {
                    try {
                        var prevComputed = (IComputed<TIn, TOut>) c;
                        if (delay > TimeSpan.Zero)
                            await clock!.DelayAsync(delay, stopToken).ConfigureAwait(false);
                        else
                            stopToken.ThrowIfCancellationRequested();
                        var nextComputed = await prevComputed.UpdateAsync(stopToken).ConfigureAwait(false);
                        var prevOutput = prevComputed.Output;
                        handler?.Invoke(nextComputed, prevOutput, invalidatedBy);
                        nextComputed!.Invalidated += OnInvalidated;
                    }
                    catch (OperationCanceledException) { }
                };
                computed.Invalidated += OnInvalidated;
                return Disposable.New(
                    (Computed: (IComputed) computed, CancellationTokenSource: stop, CompletedHandler: untypedCompletedHandler), 
                    state => {
                        try {
                            state.CancellationTokenSource.Cancel();
                        }
                        finally {
                            state.CancellationTokenSource.Dispose();
                            if (untypedCompletedHandler is Action<IComputed<TOut>> completedHandler)
                                completedHandler.Invoke(computed);
                        }
                    }); 
            }                               
        }

        public static Disposable<(IComputed, CancellationTokenSource, Delegate?)> AutoUpdate<T>(
            this IComputed<T> computed, 
            Action<IComputed<T>, Result<T>, object?>? recomputed = null,
            Action<IComputed<T>>? completed = null)
            => computed.AutoUpdate(default, null, recomputed, completed);

        public static Disposable<(IComputed, CancellationTokenSource, Delegate?)> AutoUpdate<T>(
            this IComputed<T> computed, 
            TimeSpan delay = default,
            Action<IComputed<T>, Result<T>, object?>? recomputed = null,
            Action<IComputed<T>>? completed = null)
            => computed.AutoUpdate(delay, null, recomputed, completed);

        public static Disposable<(IComputed, CancellationTokenSource, Delegate?)> AutoUpdate<T>(
            this IComputed<T> computed, 
            TimeSpan delay = default,
            IClock? clock = null,
            Action<IComputed<T>, Result<T>, object?>? recomputed = null,
            Action<IComputed<T>>? completed = null)
        {
            clock ??= RealTimeClock.Instance;
            return computed.Apply(AutoUpdateApplyHandler.Instance, (delay, clock, recomputed, completed));
        }
    }
}
