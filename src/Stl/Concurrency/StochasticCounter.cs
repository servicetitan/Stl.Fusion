using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Stl.Mathematics;
using Stl.OS;

namespace Stl.Concurrency
{
    public sealed class StochasticCounter
    {
        public const int DefaultApproximationFactor = 4;

        private long _value = 0;  
        private readonly uint _approximationMask;
        public int ApproximationStep { get; }
        public int ApproximationStepLog2 { get; }

        public long ApproximateValue {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Interlocked.Read(ref _value) << ApproximationStepLog2;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Interlocked.Exchange(ref _value, value >> ApproximationStepLog2);
        }

        public StochasticCounter() 
            : this(DefaultApproximationFactor) { }
        public StochasticCounter(int approximationFactor)
        {
            if (approximationFactor < 1)
                throw new ArgumentOutOfRangeException(nameof(approximationFactor));
            approximationFactor *= HardwareInfo.ProcessorCount;
            ApproximationStep = (int) Bits.GreaterOrEqualPowerOf2((uint) approximationFactor);
            ApproximationStepLog2 = Bits.MsbIndex((ulong) ApproximationStep);
            _approximationMask = ((uint) ApproximationStep - 1) << 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => Interlocked.Exchange(ref _value, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Increment(int random, out long approximateValue)
        {
            if ((_approximationMask & (uint) random) != 0) {
                approximateValue = 0;
                return false;
            }
            var value = Interlocked.Increment(ref _value);
            approximateValue = value << ApproximationStepLog2;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Decrement(int random, out long approximateValue)
        {
            if ((_approximationMask & (uint) random) != 0) {
                approximateValue = 0;
                return false;
            }
            var value = Interlocked.Decrement(ref _value);
            if (value < 0) {
                Interlocked.CompareExchange(ref _value, 0, value);
                value = 0;
            }
            approximateValue = value << ApproximationStepLog2;
            return true;
        }
    }
}
