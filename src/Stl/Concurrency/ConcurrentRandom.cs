namespace Stl.Concurrency;

public static class ConcurrentRandom
{
    private static readonly Random SeedRandom = new();
    [ThreadStatic]
    private static Random? _instance;

    private static Random Instance {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _instance ??= Create();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Next()
    {
#if NET7_0_OR_GREATER
        return Random.Shared.Next();
#else
        return Instance.Next();
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double NextDouble()
    {
#if NET7_0_OR_GREATER
        return Random.Shared.NextDouble();
#else
        return Instance.NextDouble();
#endif
    }

    // Private methods

    private static Random Create()
    {
        lock (SeedRandom)
            return new Random(SeedRandom.Next() + Thread.CurrentThread.ManagedThreadId);
    }
}
