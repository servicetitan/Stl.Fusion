namespace Stl.Internal;

public abstract class ValueOf
{
    private static readonly ConcurrentDictionary<Type, Func<object, ValueOf>> FactoryCache = new();
    private static readonly MethodInfo FactoryMethod =
        typeof(ValueOf).GetMethod(nameof(Factory), BindingFlags.Static | BindingFlags.NonPublic)!;

    [JsonIgnore, Newtonsoft.Json.JsonIgnore, IgnoreDataMember, MemoryPackIgnore]
    public abstract object UntypedValue { get; }

    public static ValueOf<T> New<T>(T value) => new(value);
    public static Func<object, ValueOf> GetFactory(Type type)
        => FactoryCache.GetOrAdd(type,
            t => (Func<object, ValueOf>)FactoryMethod
                .MakeGenericMethod(t)
                .CreateDelegate(typeof(Func<object, ValueOf>)));

    private static ValueOf Factory<T>(object value)
        => new ValueOf<T>((T) value);
}

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
[method: JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
public sealed partial class ValueOf<T>(T value) : ValueOf
{
    [DataMember(Order = 0), MemoryPackOrder(0)]
    public T Value { get; } = value;

#pragma warning disable CS8603
    public override object UntypedValue => Value;
#pragma warning restore CS8603

    public override string ToString()
        => $"{GetType().Name}({Value})";

    public static implicit operator T(ValueOf<T> source) => source.Value;
    public static implicit operator ValueOf<T>(T value) => new(value);
}
