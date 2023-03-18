using Stl.Generators.Internal;
using Stl.Time.Internal;

namespace Stl.Generators;

public static class ConcurrentLTagGenerator
{
    public static readonly ConcurrentGenerator<LTag> Default = New(CoarseClockHelper.RandomInt64);

    public static ConcurrentGenerator<LTag> New(long start, int concurrencyLevel = -1)
    {
        if (concurrencyLevel <= 0)
            concurrencyLevel = ConcurrentInt32Generator.DefaultConcurrencyLevel;
        var dCount = (long) Bits.GreaterOrEqualPowerOf2((uint) concurrencyLevel);
        var mCount = long.MaxValue >> 8; // Let's have some reserve @ the top of the band
        return new ConcurrentFuncBasedGenerator<LTag>(i => {
            var count = start + i;
            return () => {
                count = (count + dCount) & mCount;
                // We want to return only strictly positive LTags
                // (w/o IsSpecial flag)
                return new LTag(count + 1);
            };
        }, concurrencyLevel);
    }
}
