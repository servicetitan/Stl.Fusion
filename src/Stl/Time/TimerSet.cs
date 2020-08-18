using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Collections;

namespace Stl.Time
{
    public sealed class TimerSet<TTimer> : AsyncProcessBase
        where TTimer : notnull
    {
        public class Options
        {
            // ReSharper disable once StaticMemberInGenericType
            public static TimeSpan MinQuanta { get; } = TimeSpan.FromMilliseconds(10);
            public TimeSpan Quanta { get; set; } = TimeSpan.FromSeconds(1);
            public Action<TTimer>? FireHandler { get; set; }
            public IMomentClock? Clock { get; set; }
        }

        private readonly Action<TTimer>? _fireHandler;
        private readonly RadixHeapSet<TTimer> _timers = new RadixHeapSet<TTimer>(45);
        private readonly Moment _start;
        private readonly object _lock = new object();
        private int minPriority = 0;

        public TimeSpan Quanta { get; }
        public IMomentClock Clock { get; }
        public int Count {
            get {
                lock (_lock) return _timers.Count;
            }
        }

        public TimerSet(Options? options = null,
            Action<TTimer>? fireHandler = null,
            IMomentClock? clock = null)
        {
            options ??= new Options();
            if (options.Quanta < Options.MinQuanta)
                options.Quanta = Options.MinQuanta;
            Quanta = options.Quanta;
            Clock = clock ?? options.Clock ?? CoarseCpuClock.Instance;
            _fireHandler = fireHandler ?? options.FireHandler;
            _start = Clock.Now;
            Task.Run(RunAsync);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddOrUpdate(TTimer timer, Moment time)
        {
            lock (_lock) {
                var priority = GetPriority(time);
                _timers.AddOrUpdate(priority, timer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AddOrUpdateToEarlier(TTimer timer, Moment time)
        {
            lock (_lock) {
                var priority = GetPriority(time);
                return _timers.AddOrUpdateToLower(priority, timer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AddOrUpdateToLater(TTimer timer, Moment time)
        {
            lock (_lock) {
                var priority = GetPriority(time);
                return _timers.AddOrUpdateToHigher(priority, timer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(TTimer timer)
        {
            lock (_lock) {
                return _timers.Remove(timer, out var _);
            }
        }

        // Protected & private methods

        protected override async Task RunInternalAsync(CancellationToken cancellationToken)
        {
            var dueAt = _start + Quanta;
            for (;; dueAt += Quanta) {
                // ReSharper disable once InconsistentlySynchronizedField
                if (dueAt > Clock.Now)
                    await Clock.DelayAsync(dueAt, cancellationToken).ConfigureAwait(false);
                else
                    cancellationToken.ThrowIfCancellationRequested();
                IReadOnlyDictionary<TTimer, long> minSet;
                lock (_lock) {
                    minSet = _timers.ExtractMinSet(minPriority);
                    ++minPriority;
                }
                if (_fireHandler != null && minSet.Count != 0) {
                    foreach (var (timer, _) in minSet) {
                        try {
                            _fireHandler!.Invoke(timer);
                        }
                        catch {
                            // Intended suppression
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long GetPriority(Moment time)
        {
            var priority = (time - _start).Ticks / Quanta.Ticks;
            return Math.Max(minPriority, priority);
        }
    }
}
