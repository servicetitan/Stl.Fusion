using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Stl.Mathematics;
using Stl.OS;

namespace Stl.Concurrency
{
    public sealed class ConcurrentCounter
    {
        public static readonly int DefaultApproximationStep = 16;
        public static int DefaultConcurrencyLevel => HardwareInfo.ProcessorCountPo2;

        private readonly long _approximationStep;
        private readonly long[] _counters; 
        private long _approximateValue = 0;  
        private readonly long _concurrencyMask;

        public int ConcurrencyLevel => _counters.Length;
        public int ApproximationStep => (int) _approximationStep;

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
            concurrencyLevel = (int) (Bits.Msb((ulong) concurrencyLevel) << 1);
            _counters = new long[concurrencyLevel];
            _approximationStep = approximationStep;
            _concurrencyMask = concurrencyLevel - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Increment(int random, out long approximateValue)
        {
            var t = _approximationStep;
            ref var counter = ref _counters[random & _concurrencyMask];
            var value = Interlocked.Increment(ref counter);
            if (value >= t) {
                Interlocked.Add(ref counter, -t);
                approximateValue = Interlocked.Add(ref _approximateValue, t);
                return true;
            }
            approximateValue = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Decrement(int random, out long approximateValue)
        {
            var t = _approximationStep;
            ref var counter = ref _counters[random & _concurrencyMask];
            var value = Interlocked.Decrement(ref counter);
            if (value < 0) {
                Interlocked.Add(ref counter, t);
                approximateValue = Interlocked.Add(ref _approximateValue, -t);
                return true;
            }
            approximateValue = 0;
            return false;
        }
    }
}
