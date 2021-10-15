using Stl.Mathematics;

namespace Stl.Generators.Internal;

public sealed class ConcurrentFuncBasedGenerator<T> : ConcurrentGenerator<T>
{
    private readonly Func<T>[] _generators;

    public int ConcurrencyLevel => _generators.Length;
    public int ConcurrencyLevelMask { get; }

    public ConcurrentFuncBasedGenerator(Func<int, Func<T>> generatorFactory)
        : this(generatorFactory, ConcurrentInt32Generator.DefaultConcurrencyLevel) { }
    public ConcurrentFuncBasedGenerator(Func<int, Func<T>> generatorFactory, int concurrencyLevel)
        : this(Enumerable.Range(0, concurrencyLevel).Select(generatorFactory)) { }
    public ConcurrentFuncBasedGenerator(IEnumerable<Func<T>> generators)
        : this(generators.ToArray()) { }
    public ConcurrentFuncBasedGenerator(Func<T>[] generators)
    {
        if (!Bits.IsPowerOf2((uint) generators.Length))
            throw new ArgumentOutOfRangeException(nameof(generators));
        _generators = generators;
        ConcurrencyLevelMask = generators.Length - 1;
    }

    public override T Next(int random)
    {
        var generator = _generators[random & ConcurrencyLevelMask];
        lock (generator)
            return generator.Invoke();
    }
}
