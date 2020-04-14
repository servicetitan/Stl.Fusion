using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Purifier.Internal;
using Stl.Time;

namespace Stl.Purifier
{
    public static class ComputedBehavior
    {
        public static IObservable<IComputedWithTypedInput<TKey>> AutoRecompute<TKey>(
            this IComputedWithTypedInput<TKey> computed, 
            TimeSpan delay = default,
            IClock? clock = null)
            where TKey : notnull
        {
            clock ??= RealTimeClock.Instance;
            var stop = new CancellationTokenSource();
            var subject = new SubjectWithDisposer<IComputedWithTypedInput<TKey>, CancellationTokenSource>(
                stop, stop1 => stop1.Cancel());

            async void OnInvalidated(IComputed c) {
                var stopToken = stop!.Token;
                var error = (Exception?) null;
                try {
                    var computed1 = (IComputedWithTypedInput<TKey>) c;
                    var (function, input) = (computed1.Function, computed1.Input);
                    if (delay > TimeSpan.Zero)
                        await clock!.DelayAsync(delay, stopToken).ConfigureAwait(false);
                    else
                        await Task.Yield();
                    computed1 = await function.InvokeAsync(input, null, stopToken).ConfigureAwait(false);
                    subject!.OnNext(computed1);
                    computed1.Invalidated += OnInvalidated;
                }
                catch (TaskCanceledException e) {
                    error = e;
                    subject!.OnCompleted();
                }
                catch (Exception e) {
                    error = e;
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
}
