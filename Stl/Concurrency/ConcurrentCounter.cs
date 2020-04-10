using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Stl.Concurrency
{
    public sealed class ConcurrentCounter
    {
        public static readonly int DefaultCounterCount = Environment.ProcessorCount;

        private readonly int _updateThreshold;
        private readonly int[] _counters; 
        private long _approximateValue = 0;  

        public int CounterCount => _counters.Length;
        public int UpdateThreshold => _updateThreshold;

        public long ApproximateValue {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref _approximateValue);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Interlocked.Exchange(ref _approximateValue, value);
        }

        public long PreciseValue {
            get {
                var c = ApproximateValue;
                for (var i = 0; i < _counters.Length; i++)
                    c += Volatile.Read(ref _counters[i]);
                return c;
            }
            set {
                ApproximateValue = value;
                for (var i = 0; i < _counters.Length; i++)
                    Interlocked.Exchange(ref _counters[i], 0);
            }
        }

        public ConcurrentCounter(int updateThreshold) 
            : this(updateThreshold, DefaultCounterCount) 
        { }

        public ConcurrentCounter(int updateThreshold, int counterCount)
        {
            if (counterCount < 1)
                throw new ArgumentOutOfRangeException(nameof(counterCount));
            if (updateThreshold < 1)
                throw new ArgumentOutOfRangeException(nameof(updateThreshold));
            _counters = new int[counterCount];
            _updateThreshold = updateThreshold;
        }

        public bool Increment(int random)
        {
            var t = _updateThreshold;
            ref var counter = ref _counters[random % CounterCount];
            var value = Interlocked.Increment(ref counter);
            if (value >= t) {
                Interlocked.Add(ref counter, -t);
                Interlocked.Add(ref _approximateValue, t);
                return true;
            }
            return false;
        }

        public bool Decrement(int random)
        {
            var t = _updateThreshold;
            ref var counter = ref _counters[random % CounterCount];
            var value = Interlocked.Decrement(ref counter);
            if (value < 0) {
                Interlocked.Add(ref counter, t);
                Interlocked.Add(ref _approximateValue, -t);
                return true;
            }
            return false;
        }
    }
}
