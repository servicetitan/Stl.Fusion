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
        internal sealed class TrackChangesApplyHandler : IComputedApplyHandler<(TimeSpan, IClock, Delegate?), Disposable<CancellationTokenSource>>
        {
            public static readonly TrackChangesApplyHandler Instance = new TrackChangesApplyHandler();
            
            public Disposable<CancellationTokenSource> Apply<TIn, TOut>(IComputed<TIn, TOut> computed, (TimeSpan, IClock, Delegate?) arg) 
                where TIn : notnull
            {
                var (delay, clock, untypedHandler) = arg;
                var handler = (Action<IComputed<TOut>, Result<TOut>>?) untypedHandler;
                var stop = new CancellationTokenSource();
                var stopToken = stop.Token;

                async void OnInvalidated(IComputed c) {
                    try {
                        var prevComputed = (IComputed<TIn, TOut>) c;
                        if (delay > TimeSpan.Zero)
                            await clock!.DelayAsync(delay, stopToken).ConfigureAwait(false);
                        var nextComputed = await prevComputed.RenewAsync(stopToken).ConfigureAwait(false);
                        var prevOutput = prevComputed.Output;
                        prevComputed = null!;
                        handler?.Invoke(nextComputed, prevOutput);
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
                        cts.Dispose();
                        // var ctsCopy = cts;
                        // Task.Run(async () => {
                        //     await Task.Delay(CancellationTokenDisposeDelay, CancellationToken.None).ConfigureAwait(false);
                        //     ctsCopy.Dispose();
                        // }, CancellationToken.None);
                    }
                }); 
            }                               
        }

        public static Disposable<CancellationTokenSource> TrackChanges<T>(
            this IComputed<T> computed, 
            Action<IComputed<T>, Result<T>>? handler = null)
            => computed.TrackChanges(default, null, handler);

        public static Disposable<CancellationTokenSource> TrackChanges<T>(
            this IComputed<T> computed, 
            TimeSpan delay = default,
            Action<IComputed<T>, Result<T>>? handler = null)
            => computed.TrackChanges(delay, null, handler);

        public static Disposable<CancellationTokenSource> TrackChanges<T>(
            this IComputed<T> computed, 
            TimeSpan delay = default,
            IClock? clock = null,
            Action<IComputed<T>, Result<T>>? handler = null)
        {
            clock ??= RealTimeClock.Instance;
            return computed.Apply(TrackChangesApplyHandler.Instance, (delay, clock, handler));
        }
    }
}
