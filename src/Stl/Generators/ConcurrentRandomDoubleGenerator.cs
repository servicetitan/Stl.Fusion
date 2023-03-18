using Stl.Generators.Internal;
using Stl.OS;
using Stl.Time.Internal;

namespace Stl.Generators;

public static class ConcurrentRandomDoubleGenerator
{
    internal static int DefaultConcurrencyLevel => HardwareInfo.GetProcessorCountPo2Factor(2);

    public static readonly ConcurrentGenerator<double> Default = New(CoarseClockHelper.RandomInt32);

    public static ConcurrentGenerator<double> New(int start, int concurrencyLevel = -1)
    {
        if (concurrencyLevel <= 0)
            concurrencyLevel = DefaultConcurrencyLevel;
        return new ConcurrentFuncBasedGenerator<double>(i => {
            var random = new Random(start + i);
            return () => random.NextDouble();
        }, concurrencyLevel);
    }
}
