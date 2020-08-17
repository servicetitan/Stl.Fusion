using System;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Mathematics;

namespace Stl.Time
{
    public sealed class ConcurrentTimerSet<TTimer> : AsyncDisposableBase, ITimerSet<TTimer>
    {
        public class Options : TimerSet<TTimer>.Options
        {
            public int ConcurrencyLevel { get; set; }
        }

        private readonly Action<TTimer>? _fireHandler;
        private readonly TimerSet<TTimer>[] _timerSets;
        private readonly int _concurrencyLevelMask;

        public TimeSpan Quanta { get; }
        public IMomentClock Clock { get; }
        public int ConcurrencyLevel { get; }

        public ConcurrentTimerSet(Options? options = null,
            Action<TTimer>? fireHandler = null,
            IMomentClock? clock = null)
        {
            options ??= new Options();
            if (options.Quanta < TimeSpan.FromMilliseconds(10))
                options.Quanta = TimeSpan.FromMilliseconds(10);
            if (options.ConcurrencyLevel < 1)
                options.ConcurrencyLevel = 1;
            Quanta = options.Quanta;
            Clock = clock ?? options.Clock ?? CoarseCpuClock.Instance;
            ConcurrencyLevel = (int) Bits.GreaterOrEqualPowerOf2((ulong) Math.Max(1, options.ConcurrencyLevel));
            _fireHandler = fireHandler ?? options.FireHandler;
            _concurrencyLevelMask = ConcurrencyLevel - 1;
            _timerSets = new TimerSet<TTimer>[ConcurrencyLevel];
            for (var i = 0; i < _timerSets.Length; i++)
                _timerSets[i] = new TimerSet<TTimer>(options, _fireHandler, Clock);
        }

        protected override async ValueTask DisposeInternalAsync(bool disposing)
        {
            foreach (var timerSet in _timerSets)
                await timerSet.DisposeAsync();
        }

        public void AddOrUpdate(TTimer timer, Moment time)
        {
            var timerSet = _timerSets[(timer?.GetHashCode() ?? 0) & _concurrencyLevelMask];
            timerSet.AddOrUpdate(timer, time);
        }

        public bool Remove(TTimer timer)
        {
            var timerSet = _timerSets[(timer?.GetHashCode() ?? 0) & _concurrencyLevelMask];
            return timerSet.Remove(timer);
        }
    }
}
