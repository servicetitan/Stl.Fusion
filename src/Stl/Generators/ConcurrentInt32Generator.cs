using Stl.Generators.Internal;
using Stl.OS;
using Stl.Time.Internal;

namespace Stl.Generators;

public static class ConcurrentInt32Generator
{
    internal static int DefaultConcurrencyLevel => HardwareInfo.ProcessorCountPo2 << 1;

    public static readonly ConcurrentGenerator<int> Default = New(CoarseClockHelper.RandomInt32);

    public static ConcurrentGenerator<int> New(int start, int concurrencyLevel = -1)
    {
        if (concurrencyLevel < 0)
            concurrencyLevel = DefaultConcurrencyLevel;
        var dCount = (int) Bits.GreaterOrEqualPowerOf2((uint) concurrencyLevel);
        return new ConcurrentFuncBasedGenerator<int>(i => {
            var count = start + i;
            return () => count += dCount;
        }, concurrencyLevel);
    }
}
