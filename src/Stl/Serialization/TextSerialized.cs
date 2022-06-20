namespace Stl.Serialization;

public static class TextSerialized
{
    public static TextSerialized<TValue> New<TValue>() => new();
    public static TextSerialized<TValue> New<TValue>(TValue value) => new() { Value = value };
    public static TextSerialized<TValue> New<TValue>(string data) => new(data);
}

[DataContract]
[Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
public class TextSerialized<T> : IEquatable<TextSerialized<T>>
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

    public TextSerialized() { }
    public TextSerialized(string data)
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
            : GetSerializer().Write(value);
        _dataOption = serializedValue;
        return serializedValue;
    }

    private T Deserialize()
    {
        if (!_dataOption.IsSome(out var serializedValue))
            throw new InvalidOperationException($"{nameof(Data)} isn't set.");
        var value = serializedValue.IsNullOrEmpty()
            ? default!
            : GetSerializer().Read(serializedValue);
        _valueOption = value;
        return value;
    }

    protected virtual ITextSerializer<T> GetSerializer()
        => TextSerializer<T>.Default;

    // Equality

    public bool Equals(TextSerialized<T>? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return StringComparer.Ordinal.Equals(Data, other.Data);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        return Equals((TextSerialized<T>)obj);
    }

    public override int GetHashCode()
        => StringComparer.Ordinal.GetHashCode(Data);
    public static bool operator ==(TextSerialized<T>? left, TextSerialized<T>? right)
        => Equals(left, right);
    public static bool operator !=(TextSerialized<T>? left, TextSerialized<T>? right)
        => !Equals(left, right);
}
