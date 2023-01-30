namespace Stl.Diagnostics;

public sealed record Sampler(
    double Probability,
    Func<bool> Next)
{
    public static Sampler Always { get; } = new(1, static () => true);
    public static Sampler Never { get; } = new(0, static () => false);

    public double InverseProbability { get; } = 1d / Probability;

    public static Sampler EveryNth(long n)
    {
        if (n <= 0)
            return Never;
        if (n == 1)
            return Always;

        var limit = n * 1000_000;
        var i = 0L;
        var sampler = new Sampler(1d / n, () => {
            var j = Interlocked.Increment(ref i);
            if (j >= limit)
                Interlocked.CompareExchange(ref i, j - limit, j);
            return j % n == 0;
        });
        Thread.MemoryBarrier();
        return sampler;
    }

    public static Sampler Random(double probability, Random? random = null)
    {
        if (probability <= 0)
            return Never;
        if (probability >= 1)
            return Always;

        var rnd = random ?? new Random();
        var maxIntBasedLimit = (int)((1d + int.MaxValue) * probability - 1);
        var sampler = new Sampler(probability, () => {
            lock (rnd) {
                return rnd.Next() <= maxIntBasedLimit;
            }
        });
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
        var sampler = new Sampler(probability, () => {
            return System.Random.Shared.Next() <= maxIntBasedLimit;
        });
        Thread.MemoryBarrier();
        return sampler;
#endif
    }

    public static Sampler AlternativeRandom(double probability, Random? random = null)
    {
        if (probability <= 0)
            return Never;
        if (probability >= 1)
            return Always;

        var rnd = random ?? new Random();
        var stepSize = 1d / probability;
        var maxIntBasedLimit = (int)((1d + int.MaxValue) * probability - 1);

        // Initial step generation
        var expectedDistance = stepSize;
        var fStepCount = Math.Floor(rnd.NextDouble() * expectedDistance);
        expectedDistance -= fStepCount;
        var stepCount = 0L;
        Interlocked.Exchange(ref stepCount, (long)fStepCount);

        var sampler = new Sampler(probability, () => {
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
        });
        Thread.MemoryBarrier();
        return sampler;
    }
}
