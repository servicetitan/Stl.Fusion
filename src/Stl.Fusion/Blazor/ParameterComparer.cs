namespace Stl.Fusion.Blazor;

public abstract class ParameterComparer
{
    private static readonly ConcurrentDictionary<Type, ParameterComparer> Cache = new();
    public static ParameterComparer Default { get; } = new DefaultParameterComparer();

    public abstract bool AreEqual(object? oldValue, object? newValue);

    public static ParameterComparer Get(Type? comparerType) =>
        comparerType == null
            ? Default
            : Cache.GetOrAdd(comparerType, comparerType1 => {
                if (!typeof(ParameterComparer).IsAssignableFrom(comparerType1))
                    throw new ArgumentOutOfRangeException(nameof(comparerType));
                return (ParameterComparer)comparerType1.CreateInstance();
            });
}
