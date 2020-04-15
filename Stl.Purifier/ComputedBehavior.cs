using System;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Stl.Purifier.Internal;
using Stl.Time;

namespace Stl.Purifier
{
    public static class ComputedBehavior
    {
        internal sealed class AutoRecomputeApplyHandler : IComputedApplyHandler<(TimeSpan, IClock, Action<IComputed>?), Disposable<CancellationTokenSource>>
        {
            private static readonly TimeSpan CancellationTokenDisposeDelay = TimeSpan.FromSeconds(5); 
            public static readonly AutoRecomputeApplyHandler Instance = new AutoRecomputeApplyHandler();
            
            public Disposable<CancellationTokenSource> Apply<TIn, TOut>(IComputed<TIn, TOut> computed, (TimeSpan, IClock, Action<IComputed>?) arg) 
                where TIn : notnull
            {
                var (delay, clock, handler) = arg;
                var stop = new CancellationTokenSource();
                var stopToken = stop.Token;

                async void OnInvalidated(IComputed c) {
                    try {
                        var prevComputed = (IComputed<TIn, TOut>) c;
                        var (function, input) = (prevComputed.Function, prevComputed.Input);
                        if (delay > TimeSpan.Zero)
                            await clock!.DelayAsync(delay, stopToken).ConfigureAwait(false);
                        else
                            await Task.Yield();
                        var nextComputed = await function
                            .InvokeAsync(input, null, stopToken)
                            .ConfigureAwait(false);
                        handler?.Invoke(nextComputed);
                        nextComputed.Invalidated += OnInvalidated;
                    }
                    catch (OperationCanceledException) { }
                };
                computed.Invalidated += OnInvalidated;
                return Disposable.New(stop, cts => {
                    try {
                        cts.Cancel(true);
                    }
                    finally {
                        var ctsCopy = cts;
                        Task.Run(async () => {
                            await Task.Delay(CancellationTokenDisposeDelay, CancellationToken.None).ConfigureAwait(false);
                            ctsCopy.Dispose();
                        }, CancellationToken.None);
                    }
                }); 
            }                               
        }

        public static Disposable<CancellationTokenSource> AutoRecompute(
            this IComputed computed, 
            Action<IComputed>? handler = null)
            => computed.AutoRecompute(default, null, handler);

        public static Disposable<CancellationTokenSource> AutoRecompute(
            this IComputed computed, 
            TimeSpan delay = default,
            Action<IComputed>? handler = null)
            => computed.AutoRecompute(delay, null, handler);

        public static Disposable<CancellationTokenSource> AutoRecompute(
            this IComputed computed, 
            TimeSpan delay = default,
            IClock? clock = null,
            Action<IComputed>? handler = null)
        {
            clock ??= RealTimeClock.Instance;
            return computed.Apply(AutoRecomputeApplyHandler.Instance, (delay, clock, handler));
        }
    }
}
