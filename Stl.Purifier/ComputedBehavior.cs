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
        internal sealed class AutoRecomputeApplyHandler : IComputedApplyHandler<(TimeSpan, IClock), SubjectBase<IComputed>>
        {
            public static readonly AutoRecomputeApplyHandler Instance = new AutoRecomputeApplyHandler();
            
            public SubjectBase<IComputed> Apply<TIn, TOut>(IComputed<TIn, TOut> computed, (TimeSpan, IClock) arg) 
                where TIn : notnull
            {
                var (delay, clock) = arg;
                var stop = new CancellationTokenSource();
                var subject = new SubjectWithDisposer<IComputed, CancellationTokenSource>(
                    stop, stop1 => stop1.Cancel());

                async void OnInvalidated(IComputed c) {
                    var stopToken = stop!.Token;
                    var error = (Exception?) null;
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
                        if (!subject!.IsDisposed)
                            subject!.OnNext(nextComputed);
                        nextComputed.Invalidated += OnInvalidated;
                    }
                    catch (TaskCanceledException e) {
                        error = e;
                        if (!subject!.IsDisposed)
                            subject?.OnCompleted();
                    }
                    catch (OperationCanceledException e) {
                        error = e;
                        if (!subject!.IsDisposed)
                            subject!.OnCompleted();
                    }
                    catch (Exception e) {
                        error = e;
                        if (!subject!.IsDisposed)
                            subject!.OnError(e);
                    }
                    finally {
                        if (error != null)
                            stop.Dispose();
                    }
                };

                subject.OnNext(computed);
                computed.Invalidated += OnInvalidated;
                return subject; 
            }
        }

        public static SubjectBase<IComputed> AutoRecompute(
            this IComputed computed, 
            TimeSpan delay = default,
            IClock? clock = null)
        {
            clock ??= RealTimeClock.Instance;
            return computed.Apply(AutoRecomputeApplyHandler.Instance, (delay, clock));
        }
    }
}
