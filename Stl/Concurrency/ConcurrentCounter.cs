using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Stl.OS;

namespace Stl.Concurrency
{
    public sealed class ConcurrentCounter
    {
        public static readonly int DefaultApproximationStep = 16;
        public static int DefaultConcurrencyLevel => HardwareInfo.ProcessorCount;

        private readonly int _approximationStep;
        private readonly int[] _counters; 
        private long _approximateValue = 0;  

        public int ConcurrencyLevel => _counters.Length;
        public int ApproximationStep => _approximationStep;

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

        public ConcurrentCounter() 
            : this(DefaultApproximationStep) { }
        public ConcurrentCounter(int approximationStep) 
            : this(approximationStep, DefaultConcurrencyLevel) { }
        public ConcurrentCounter(int approximationStep, int concurrencyLevel)
        {
            if (concurrencyLevel < 1)
                throw new ArgumentOutOfRangeException(nameof(concurrencyLevel));
            if (approximationStep < 1)
                throw new ArgumentOutOfRangeException(nameof(approximationStep));
            _counters = new int[concurrencyLevel];
            _approximationStep = approximationStep;
        }

        public Option<long> Increment(int random)
        {
            var t = _approximationStep;
            ref var counter = ref _counters[random % ConcurrencyLevel];
            var value = Interlocked.Increment(ref counter);
            if (value >= t) {
                Interlocked.Add(ref counter, -t);
                return Interlocked.Add(ref _approximateValue, t);
            }
            return Option<long>.None;
        }

        public Option<long> Decrement(int random)
        {
            var t = _approximationStep;
            ref var counter = ref _counters[random % ConcurrencyLevel];
            var value = Interlocked.Decrement(ref counter);
            if (value < 0) {
                Interlocked.Add(ref counter, t);
                return Interlocked.Add(ref _approximateValue, -t);
            }
            return Option<long>.None;
        }
    }
}
