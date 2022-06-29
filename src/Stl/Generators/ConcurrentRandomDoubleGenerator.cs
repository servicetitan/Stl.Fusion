using Stl.Generators.Internal;
using Stl.OS;
using Stl.Time.Internal;

namespace Stl.Generators;

public static class ConcurrentRandomDoubleGenerator
{
    internal static int DefaultConcurrencyLevel => HardwareInfo.ProcessorCountPo2 << 1;

    public static readonly ConcurrentGenerator<double> Default = New(CoarseClockHelper.RandomInt32);

    public static ConcurrentGenerator<double> New(int start, int concurrencyLevel = -1)
    {
        if (concurrencyLevel < 0)
            concurrencyLevel = DefaultConcurrencyLevel;
        var dCount = (int) Bits.GreaterOrEqualPowerOf2((uint) concurrencyLevel);
        return new ConcurrentFuncBasedGenerator<double>(i => {
            var random = new Random(start + i);
            return () => random.NextDouble();
        });
    }
}
