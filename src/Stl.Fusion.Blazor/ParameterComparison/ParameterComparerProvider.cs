namespace Stl.Fusion.Blazor;

public class ParameterComparerProvider
{
    private static readonly ConcurrentDictionary<Type, ParameterComparer> Cache = new();

    public static ParameterComparerProvider Instance { get; set; } = new();

    public static ParameterComparer Get(Type? comparerType)
    {
        if (comparerType == null)
            return ParameterComparer.Default;

        return Cache.GetOrAdd(comparerType, static comparerType1 => {
            if (!typeof(ParameterComparer).IsAssignableFrom(comparerType1))
                throw new ArgumentOutOfRangeException(nameof(comparerType));
            return (ParameterComparer)comparerType1.CreateInstance();
        });
    }

    public virtual ParameterComparer Get(PropertyInfo property)
    {
        var comparerType = GetComparerType(property);
        return Get(comparerType);
    }

    public virtual Type? GetComparerType(PropertyInfo property)
    {
        var type = property.GetCustomAttribute<ParameterComparerAttribute>(true)?.ComparerType;
        if (type != null)
            return type;

        type = GetDefaultComparerType(property);
        if (type != null)
            return type;

        type = property.PropertyType.GetCustomAttribute<ParameterComparerAttribute>(true)?.ComparerType;
        if (type != null)
            return type;

        type = property.DeclaringType?.GetCustomAttribute<ParameterComparerAttribute>(true)?.ComparerType;
        if (type != null)
            return type;

        return null;
    }

    protected virtual Type? GetDefaultComparerType(PropertyInfo property)
    {
        var propertyType = property.PropertyType;
        if (propertyType == typeof(Symbol))
            return typeof(ByValueParameterComparer);

        return null;
    }
}
