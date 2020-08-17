using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Collections;

namespace Stl.Time
{
    public interface ITimerSet<in TTimer>
    {
        void AddOrUpdate(TTimer timer, Moment time);
        bool Remove(TTimer timer);
    }

    public sealed class TimerSet<TTimer> : AsyncProcessBase, ITimerSet<TTimer>
    {
        public class Options
        {
            public TimeSpan Quanta { get; set; } = TimeSpan.FromSeconds(1);
            public Action<TTimer>? FireHandler { get; set; }
            public IMomentClock? Clock { get; set; }
        }

        private readonly Action<TTimer>? _fireHandler;
        private readonly Dictionary<TTimer, Moment> _timers = new Dictionary<TTimer, Moment>();
        private readonly RadixHeap<TTimer> _heap = new RadixHeap<TTimer>(40);
        private readonly Moment _start;
        private readonly object _lock = new object();

        public TimeSpan Quanta { get; }
        public IMomentClock Clock { get; }

        public TimerSet(Options? options = null,
            Action<TTimer>? fireHandler = null,
            IMomentClock? clock = null)
        {
            options ??= new Options();
            if (options.Quanta < TimeSpan.FromMilliseconds(10))
                options.Quanta = TimeSpan.FromMilliseconds(10);
            Quanta = options.Quanta;
            Clock = clock ?? options.Clock ?? CoarseCpuClock.Instance;
            _fireHandler = fireHandler ?? options.FireHandler;
            _start = Clock.Now;
            Task.Run(RunAsync);
        }

        public void AddOrUpdate(TTimer timer, Moment time)
        {
            lock (_lock) {
                if (_timers.TryGetValue(timer, out var oldTime))
                    _heap.Remove(timer, GetPriority(oldTime));
                _timers.Add(timer, time);
                _heap.Add(timer, GetPriority(time));
            }
        }

        public bool Remove(TTimer timer)
        {
            lock (_lock) {
                if (_timers.TryGetValue(timer, out var oldTime)) {
                    _heap.Remove(timer, GetPriority(oldTime));
                    return true;
                }
                return false;
            }
        }

        // Protected & private methods

        protected override async Task RunInternalAsync(CancellationToken cancellationToken)
        {
            var toFire = _fireHandler != null ? new List<TTimer>() : null;
            for (var priority = 0L;; priority++) {
                // ReSharper disable once InconsistentlySynchronizedField
                var dueAt = _start + Quanta * priority;
                if (dueAt > Clock.Now)
                    await Clock.DelayAsync(dueAt, cancellationToken).ConfigureAwait(false);
                else
                    cancellationToken.ThrowIfCancellationRequested();
                lock (_lock) {
                    while (!_heap.IsEmpty && _heap.MinPriority <= priority) {
                        var minSet = _heap.RemoveAllMin();
                        foreach (var (timer, _) in minSet) {
                            _timers.Remove(timer);
                            toFire?.Add(timer);
                        }
                    }
                }
                if (_fireHandler != null && toFire != null) {
                    foreach (var timer in toFire) {
                        try {
                            _fireHandler.Invoke(timer);
                        }
                        catch {
                            // Intended suppression
                        }
                    }
                    toFire.Clear();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long GetPriority(Moment time)
            => (time - _start).Ticks / Quanta.Ticks;
    }
}
