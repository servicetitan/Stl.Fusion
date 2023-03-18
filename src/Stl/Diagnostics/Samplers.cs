using System.Globalization;
using Stl.OS;

namespace Stl.Diagnostics;

public sealed record Sampler(
    string Id,
    double Probability,
    Func<bool> Next,
    Func<Sampler> Duplicate)
{
#pragma warning disable CS8603
    public static Sampler Always { get; } =
        new(nameof(Always), 1, static () => true, () => Always);
    public static Sampler Never { get; } =
        new(nameof(Never), 0, static () => false, () => Never);
#pragma warning restore CS8603

    public double InverseProbability { get; } = 1d / Probability;

    public override string ToString()
        => Id.EndsWith(")", StringComparison.Ordinal)
            ? Id // .ToConcurrent(...)-like case, the probability is already in Id there
            : $"{Id}({Probability.ToString("P1",CultureInfo.InvariantCulture)})";

    public Sampler ToConcurrent(int concurrencyLevel = -1)
    {
        if (concurrencyLevel <= 0)
            concurrencyLevel = HardwareInfo.GetProcessorCountPo2Factor(2);
        concurrencyLevel = (int)Bits.GreaterOrEqualPowerOf2((ulong)concurrencyLevel);

        var samplers = new Sampler[concurrencyLevel];
        for (var i = 0; i < concurrencyLevel; i++)
            samplers[i] = Duplicate();

        var concurrencyMask = concurrencyLevel - 1;
        var name = $"{ToString()}.{nameof(ToConcurrent)}({concurrencyLevel})";
        var sampler = new Sampler(name, Probability, () => {
            var sampler = samplers[Thread.CurrentThread.ManagedThreadId % concurrencyMask];
            return sampler.Next();
        }, () => ToConcurrent());

        Thread.MemoryBarrier();
        return sampler;
    }

    public static Sampler EveryNth(long n)
    {
        if (n <= 0)
            return Never;
        if (n == 1)
            return Always;

        var i = 0L;
        Sampler sampler;
        if (Bits.IsPowerOf2((ulong)n)) { // Faster version: use binary and w/ mask
            var mask = n - 1;
            sampler = new Sampler(nameof(EveryNth), 1d / n, () => {
                var j = Interlocked.Increment(ref i);
                return (j & mask) == 0;
            }, () => EveryNth(n));
        }
        else { // Slower version: use modulo
            var limit = n * 1000_000;
            sampler = new Sampler(nameof(EveryNth), 1d / n, () => {
                var j = Interlocked.Increment(ref i);
                if (j >= limit)
                    Interlocked.CompareExchange(ref i, j - limit, j);
                return j % n == 0;
            }, () => EveryNth(n));
        }

        Thread.MemoryBarrier();
        return sampler;
    }

    public static Sampler Random(double probability)
    {
        if (probability <= 0)
            return Never;
        if (probability >= 1)
            return Always;

        var rnd = new Random();
        var maxIntBasedLimit = (int)((1d + int.MaxValue) * probability - 1);
        var sampler = new Sampler(nameof(Random), probability, () => {
            lock (rnd) {
                return rnd.Next() <= maxIntBasedLimit;
            }
        }, () => Random(probability));
        Thread.MemoryBarrier();
        return sampler;
    }

    public static Sampler RandomShared(double probability)
    {
        if (probability <= 0)
            return Never;
        if (probability >= 1)
            return Always;

#if !NET6_0_OR_GREATER
        return Random(probability);
#else
        var maxIntBasedLimit = (int)((1d + int.MaxValue) * probability - 1);
        var sampler = new Sampler(nameof(RandomShared), probability, () => {
            return System.Random.Shared.Next() <= maxIntBasedLimit;
        }, () => RandomShared(probability));
        Thread.MemoryBarrier();
        return sampler;
#endif
    }

    public static Sampler AlternativeRandom(double probability)
    {
        if (probability <= 0)
            return Never;
        if (probability >= 1)
            return Always;

        var rnd = new Random();
        var stepSize = 1d / probability;
        var maxIntBasedLimit = (int)((1d + int.MaxValue) * probability - 1);

        // Initial step generation
        var expectedDistance = stepSize;
        var fStepCount = Math.Floor(rnd.NextDouble() * expectedDistance);
        expectedDistance -= fStepCount;
        var stepCount = 0L;
        Interlocked.Exchange(ref stepCount, (long)fStepCount);

        var sampler = new Sampler(nameof(AlternativeRandom), probability, () => {
            var c = Interlocked.Decrement(ref stepCount);
            if (c > 0)
                return false;

            lock (rnd) {
                if (c != 0) {
                    // We already "skipped through" a case when we could return true,
                    // so the best we can do is to resort to a new random choice
                    return rnd.Next() <= maxIntBasedLimit;
                }

                // Thread that "won" this will have to update stepCount
                expectedDistance += stepSize;
                fStepCount = Math.Floor(rnd.NextDouble() * expectedDistance);
                expectedDistance -= fStepCount;
                Interlocked.Exchange(ref stepCount, (long)fStepCount);
                return true;
            }
        }, () => AlternativeRandom(probability));
        Thread.MemoryBarrier();
        return sampler;
    }

    // This record relies on reference-based equality
    public bool Equals(Sampler? other) => ReferenceEquals(this, other);
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
}
