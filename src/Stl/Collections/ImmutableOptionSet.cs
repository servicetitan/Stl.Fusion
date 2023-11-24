using System.Diagnostics.CodeAnalysis;
using Stl.Internal;

namespace Stl.Collections;

#if !NET5_0
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
[StructLayout(LayoutKind.Auto)]
[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
[Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
[method: Newtonsoft.Json.JsonConstructor]
public readonly partial struct ImmutableOptionSet(
    ImmutableDictionary<Symbol, object>? items
    ) : IServiceProvider, IEquatable<ImmutableOptionSet>
{
    public static readonly ImmutableOptionSet Empty = new(ImmutableDictionary<Symbol, object>.Empty);

    private readonly ImmutableDictionary<Symbol, object>? _items = items ?? ImmutableDictionary<Symbol, object>.Empty;

    [JsonIgnore, MemoryPackIgnore]
    public ImmutableDictionary<Symbol, object> Items
        => _items ?? ImmutableDictionary<Symbol, object>.Empty;

    [DataMember(Order = 0), MemoryPackOrder(0)]
    [JsonPropertyName(nameof(Items)), Newtonsoft.Json.JsonIgnore]
    public Dictionary<string, NewtonsoftJsonSerialized<object>> JsonCompatibleItems
        => Items.ToDictionary(
            p => p.Key.Value,
            p => NewtonsoftJsonSerialized.New(p.Value),
            StringComparer.Ordinal);

    public object? this[Symbol key] => Items.TryGetValue(key, out var v) ? v : null;
    public object? this[Type optionType] => this[optionType.ToSymbol()];

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    [JsonConstructor, MemoryPackConstructor]
    public ImmutableOptionSet(Dictionary<string, NewtonsoftJsonSerialized<object>>? jsonCompatibleItems)
        : this(jsonCompatibleItems?.ToImmutableDictionary(
            p => (Symbol) p.Key,
            p => p.Value.Value))
    { }

    public object? GetService(Type serviceType)
        => this[serviceType];

    public bool Contains(Type optionType)
        => this[optionType] != null;

    public bool Contains<T>()
        => this[typeof(T)] != null;

    public bool TryGet<T>(out T value)
    {
        var objValue = this[typeof(T)];
        if (objValue == null) {
            value = default!;
            return false;
        }
        value = (T) objValue;
        return true;
    }

    public T? Get<T>()
        where T : class
    {
        var value = this[typeof(T)];
        return (T?) value;
    }

    public T GetOrDefault<T>(T @default = default!)
    {
        var value = this[typeof(T)];
        return value != null ? (T) value : @default;
    }

    // ReSharper disable once HeapView.PossibleBoxingAllocation
    public ImmutableOptionSet Set<T>(T value) => Set(typeof(T), value);
    public ImmutableOptionSet Set(Type optionType, object? value)
        => Set(optionType.ToSymbol(), value);
    public ImmutableOptionSet Set(Symbol key, object? value)
        => new(value != null ? Items.SetItem(key, value) : Items.Remove(key));

    public ImmutableOptionSet SetMany(ImmutableOptionSet overrides)
        => SetMany(overrides.Items!);
    public ImmutableOptionSet SetMany(IEnumerable<KeyValuePair<Symbol, object?>> overrides)
    {
        var result = this;
        foreach (var (key, value) in overrides)
            result = result.Set(key, value);
        return result;
    }

    public ImmutableOptionSet Remove<T>() => Set(typeof(T), null);

    public ImmutableOptionSet Replace<T>(T expectedValue, T value)
    {
        var key = typeof(T).ToSymbol();
        var currentValue = (T?) this[key];
        if (!EqualityComparer<T>.Default.Equals(currentValue!, expectedValue))
            return this;
        return Set(key, value);
    }

    // Equality

    public bool Equals(ImmutableOptionSet other) => Equals(Items, other.Items);
    public override bool Equals(object? obj) => obj is ImmutableOptionSet other && Equals(other);
    public override int GetHashCode() => Items.GetHashCode();
    public static bool operator ==(ImmutableOptionSet left, ImmutableOptionSet right) => left.Equals(right);
    public static bool operator !=(ImmutableOptionSet left, ImmutableOptionSet right) => !left.Equals(right);
}
