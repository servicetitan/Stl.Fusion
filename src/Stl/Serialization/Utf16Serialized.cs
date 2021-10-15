using System.Text.Json.Serialization;

namespace Stl.Serialization;

public static class Utf16Serialized
{
    public static Utf16Serialized<TValue> New<TValue>() => new();
    public static Utf16Serialized<TValue> New<TValue>(TValue value) => new() { Value = value };
    public static Utf16Serialized<TValue> New<TValue>(string data) => new(data);
}

[DataContract]
[Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
public class Utf16Serialized<T> : IEquatable<Utf16Serialized<T>>
{
    private Option<T> _valueOption;
    private Option<string> _dataOption;

    [JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public T Value {
        get => _valueOption.IsSome(out var v) ? v : Deserialize();
        set {
            _valueOption = value;
            _dataOption = Option<string>.None;
        }
    }

    [DataMember(Order = 0)]
    public string Data {
        get => _dataOption.IsSome(out var v) ? v : Serialize();
        set {
            _valueOption = Option<T>.None;
            _dataOption = value;
        }
    }

    // ToString

    public Utf16Serialized() { }
    public Utf16Serialized(string data)
        => _dataOption = data;

    public override string ToString()
        => $"{GetType().Name} {{ Data = {JsonFormatter.Format(Data)} }}";

    // Private & protected methods

    private string Serialize()
    {
        if (!_valueOption.IsSome(out var value))
            throw new InvalidOperationException($"{nameof(Value)} isn't set.");
        var serializedValue = !typeof(T).IsValueType && ReferenceEquals(value, null)
            ? ""
            : GetSerializer().Writer.Write(value);
        _dataOption = serializedValue;
        return serializedValue;
    }

    private T Deserialize()
    {
        if (!_dataOption.IsSome(out var serializedValue))
            throw new InvalidOperationException($"{nameof(Data)} isn't set.");
        var value = serializedValue.IsNullOrEmpty()
            ? default!
            : GetSerializer().Reader.Read(serializedValue);
        _valueOption = value;
        return value;
    }

    protected virtual IUtf16Serializer<T> GetSerializer()
        => Utf16Serializer<T>.Default;

    // Equality

    public bool Equals(Utf16Serialized<T>? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Data.Equals(other.Data);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        return Equals((Utf16Serialized<T>)obj);
    }

    public override int GetHashCode()
        => Data.GetHashCode();
    public static bool operator ==(Utf16Serialized<T>? left, Utf16Serialized<T>? right)
        => Equals(left, right);
    public static bool operator !=(Utf16Serialized<T>? left, Utf16Serialized<T>? right)
        => !Equals(left, right);
}
