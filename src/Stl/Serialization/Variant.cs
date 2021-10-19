using System.Text.Json.Serialization;

namespace Stl.Serialization;

[DataContract]
public abstract class Variant<TValue> : IEquatable<Variant<TValue>>
    where TValue : class
{
    private TValue? _value;

    [IgnoreDataMember]
    [JsonIgnore]
    public TValue? Value {
        get => _value;
        init => _value = value;
    }

    protected TExactValue? Get<TExactValue>()
        where TExactValue : class, TValue
    {
        var value = Value;
        if (value?.GetType() != typeof(TExactValue))
            return null;
        return (TExactValue) value;
    }

    protected void Set(TValue? value)
        => _value = value;

    protected Variant() { }
    protected Variant(TValue? value) => Value = value;

    public override string ToString()
        => $"{GetType().Name} {{ Value = {Value} }}";

    // Equality

    public bool Equals(Variant<TValue>? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        return Equals(Value, other.Value);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (obj.GetType() != GetType())
            return false;
        return Equals((Variant<TValue>)obj);
    }

    public override int GetHashCode()
        => Value?.GetHashCode() ?? 0;

    public static bool operator ==(Variant<TValue>? left, Variant<TValue>? right)
        => Equals(left, right);
    public static bool operator !=(Variant<TValue>? left, Variant<TValue>? right)
        => !Equals(left, right);
}
