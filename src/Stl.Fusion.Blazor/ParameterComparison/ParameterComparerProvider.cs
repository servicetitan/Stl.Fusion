using System.Diagnostics.CodeAnalysis;
using Stl.Internal;

namespace Stl.Fusion.Blazor;

public class ParameterComparerProvider
{
    private static readonly ConcurrentDictionary<Type, ParameterComparer> Cache = new();

    public static ParameterComparerProvider Instance { get; set; } = new();

    public bool UseByValueParameterComparerForEnumProperties { get; init; } = true;
    public Dictionary<Type, Type> KnownComparerTypes { get; init; } = new() {
        { typeof(Symbol), typeof(ByValueParameterComparer) },
        { typeof(TimeSpan), typeof(ByValueParameterComparer) },
        { typeof(Moment), typeof(ByValueParameterComparer) },
        { typeof(DateTimeOffset), typeof(ByValueParameterComparer) },
#if NET6_0_OR_GREATER
        { typeof(DateOnly), typeof(ByValueParameterComparer) },
        { typeof(TimeOnly), typeof(ByValueParameterComparer) },
#endif
    };

    public static ParameterComparer Get(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? comparerType)
    {
        if (comparerType == null)
            return DefaultParameterComparer.Instance;

        return Cache.GetOrAdd(comparerType, static comparerType1 => {
            if (!typeof(ParameterComparer).IsAssignableFrom(comparerType1))
                throw new ArgumentOutOfRangeException(nameof(comparerType));
#pragma warning disable IL2067
            return (ParameterComparer)comparerType1.CreateInstance();
#pragma warning restore IL2067
        });
    }

    [RequiresUnreferencedCode(UnreferencedCode.Reflection)]
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

        type = GetKnownComparerType(property);
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

    protected virtual Type? GetKnownComparerType(PropertyInfo property)
    {
        var propertyType = property.PropertyType;
        if (UseByValueParameterComparerForEnumProperties && propertyType.IsEnum)
            return typeof(ByValueParameterComparer);

        return KnownComparerTypes.GetValueOrDefault(propertyType);
    }
}
