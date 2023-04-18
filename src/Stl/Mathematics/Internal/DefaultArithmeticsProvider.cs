namespace Stl.Mathematics.Internal;

public class DefaultArithmeticsProvider : ArithmeticsProvider
{
    private readonly ConcurrentDictionary<Type, IArithmetics> _cache = new();

    public sealed override Arithmetics<T> Get<T>()
        => (Arithmetics<T>) _cache.GetOrAdd(typeof(T), Create);

    public static IArithmetics Create(Type type)
        => type switch {
            _ when type == typeof(int) => new IntArithmetics(),
            _ when type == typeof(long) => new LongArithmetics(),
            _ when type == typeof(double) => new DoubleArithmetics(),
            _ when type == typeof(Moment) => new MomentArithmetics(),
            _ when type == typeof(TimeSpan) => new TimeSpanArithmetics(),
            _ => throw Errors.CantFindArithmetics(type)
        };
}
