using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Stl.Mathematics;
using Stl.OS;
using Stl.Time;

namespace Stl.Concurrency
{
    public sealed class StochasticCounter2
    {
        public const int DefaultApproximationFactor = 16;

        private long _value = 0;  
        private readonly uint[] _approximationMasks;
        public int ApproximationStep { get; }
        public int ApproximationStepLog2 { get; }

        public long ApproximateValue {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Interlocked.Read(ref _value) << ApproximationStepLog2;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Interlocked.Exchange(ref _value, value >> ApproximationStepLog2);
        }

        public StochasticCounter2() 
            : this(DefaultApproximationFactor) { }
        public StochasticCounter2(int approximationFactor)
        {
            if (approximationFactor < 1)
                throw new ArgumentOutOfRangeException(nameof(approximationFactor));
            approximationFactor *= HardwareInfo.ProcessorCount;
            ApproximationStep = (int) (Bits.Msb((ulong) approximationFactor) << 1);
            ApproximationStepLog2 = Bits.MsbIndex((ulong) ApproximationStep);
            var mask = (uint) ApproximationStep - 1;

            // Computing 32 bit-shifted masks
            var masks = new uint[32];
            for (var i = 0; i < masks.Length; i++) {
                masks[i] = mask;
                mask = (mask << 1) | (mask >> 31); 
            }
            _approximationMasks = masks;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool Increment(int random, out long approximateValue)
        {
            fixed (uint* pMasks = _approximationMasks) {
                if ((pMasks[31 & IntMoment.Clock.EpochOffsetUnits] & (uint) random) != 0) {
                    approximateValue = 0;
                    return false;
                }
            }
            var value = Interlocked.Increment(ref _value);
            approximateValue = value << ApproximationStepLog2;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool Decrement(int random, out long approximateValue)
        {
            fixed (uint* pMasks = _approximationMasks) {
                if ((pMasks[31 & IntMoment.Clock.EpochOffsetUnits] & (uint) random) != 0) {
                    approximateValue = 0;
                    return false;
                }
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
