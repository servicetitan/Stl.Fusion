using System.Text.Json.Serialization;

namespace Stl.Collections;

[DataContract]
[Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
public readonly struct ImmutableOptionSet : IServiceProvider, IEquatable<ImmutableOptionSet>
{
    public static readonly ImmutableOptionSet Empty = new(ImmutableDictionary<Symbol, object>.Empty);

    private readonly ImmutableDictionary<Symbol, object>? _items;

    [JsonIgnore]
    public ImmutableDictionary<Symbol, object> Items
        => _items ?? ImmutableDictionary<Symbol, object>.Empty;

    [DataMember(Order = 0)]
    [JsonPropertyName(nameof(Items)),  Newtonsoft.Json.JsonIgnore]
    public Dictionary<string, NewtonsoftJsonSerialized<object>> JsonCompatibleItems
        => Items.ToDictionary(
            p => p.Key.Value,
            p => NewtonsoftJsonSerialized.New(p.Value),
            StringComparer.Ordinal);

    public object? this[Symbol key] => Items.TryGetValue(key, out var v) ? v : null;
    public object? this[Type type] => this[type.ToSymbol()];

    [Newtonsoft.Json.JsonConstructor]
    public ImmutableOptionSet(ImmutableDictionary<Symbol, object>? items)
        => _items = items ?? ImmutableDictionary<Symbol, object>.Empty;

    [JsonConstructor]
    public ImmutableOptionSet(Dictionary<string, NewtonsoftJsonSerialized<object>>? jsonCompatibleItems)
        : this(jsonCompatibleItems?.ToImmutableDictionary(
            p => (Symbol) p.Key,
            p => p.Value.Value))
    { }

    public object? GetService(Type serviceType)
        => this[serviceType];

    public T? TryGet<T>()
        => (T?) this[typeof(T)]!;

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

    public T Get<T>()
    {
        var value = this[typeof(T)];
        if (value == null)
            throw new KeyNotFoundException();
        return (T) value;
    }

    public T GetOrDefault<T>(T @default)
    {
        var value = this[typeof(T)];
        return value == null ? @default : (T) value;
    }

    public ImmutableOptionSet Set(Symbol key, object? value)
        => new(value != null ? Items.SetItem(key, value) : Items.Remove(key));
    public ImmutableOptionSet Set(Type type, object? value)
        => Set(type.ToSymbol(), value);
    // ReSharper disable once HeapView.PossibleBoxingAllocation
    public ImmutableOptionSet Set<T>(T value) => Set(typeof(T), value);
    public ImmutableOptionSet Remove<T>() => Set(typeof(T), null);

    // Equality

    public bool Equals(ImmutableOptionSet other) => Equals(Items, other.Items);
    public override bool Equals(object? obj) => obj is ImmutableOptionSet other && Equals(other);
    public override int GetHashCode() => Items.GetHashCode();
    public static bool operator ==(ImmutableOptionSet left, ImmutableOptionSet right) => left.Equals(right);
    public static bool operator !=(ImmutableOptionSet left, ImmutableOptionSet right) => !left.Equals(right);
}
