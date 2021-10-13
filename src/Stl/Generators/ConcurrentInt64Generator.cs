using Stl.Generators.Internal;
using Stl.Mathematics;
using Stl.Time.Internal;

namespace Stl.Generators
{
    public static class ConcurrentInt64Generator
    {
        public static readonly ConcurrentGenerator<long> Default = New(CoarseClockHelper.RandomInt64);

        public static ConcurrentGenerator<long> New(long start, int concurrencyLevel = -1)
        {
            if (concurrencyLevel < 0)
                concurrencyLevel = ConcurrentInt32Generator.DefaultConcurrencyLevel;
            var dCount = (long) Bits.GreaterOrEqualPowerOf2((uint) concurrencyLevel);
            return new ConcurrentFuncBasedGenerator<long>(i => {
                var count = start + i;
                return () => count += dCount;
            });
        }
    }
}
