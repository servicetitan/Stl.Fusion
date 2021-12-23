using System.Text.Json.Serialization;

namespace Stl.Serialization;

public static class ByteSerialized
{
    public static ByteSerialized<TValue> New<TValue>() => new();
    public static ByteSerialized<TValue> New<TValue>(TValue value) => new() { Value = value };
    public static ByteSerialized<TValue> New<TValue>(byte[] data) => new(data);
}

[DataContract]
[Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
public class ByteSerialized<T> : IEquatable<ByteSerialized<T>>
{
    private Option<T> _valueOption;
    private Option<byte[]> _dataOption;

    [JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public T Value {
        get => _valueOption.IsSome(out var v) ? v : Deserialize();
        set {
            _valueOption = value;
            _dataOption = Option<byte[]>.None;
        }
    }

    [DataMember(Order = 0)]
    public byte[] Data {
        get => _dataOption.IsSome(out var v) ? v : Serialize();
        set {
            _valueOption = Option<T>.None;
            _dataOption = value;
        }
    }

    public ByteSerialized() { }
    public ByteSerialized(byte[] data)
        => _dataOption = data;

    // ToString

    public override string ToString()
        => $"{GetType().Name} {{ Data = {JsonFormatter.Format(Data)} }}";

    // Private & protected methods

    private byte[] Serialize()
    {
        if (!_valueOption.IsSome(out var value))
            throw new InvalidOperationException($"{nameof(Value)} isn't set.");
        byte[] serializedValue;
        if (!typeof(T).IsValueType && ReferenceEquals(value, null)) {
            serializedValue = Array.Empty<byte>();
        } else {
            using var bufferWriter = GetSerializer().Write(value);
            serializedValue = bufferWriter.WrittenSpan.ToArray();
        }
        _dataOption = serializedValue;
        return serializedValue;
    }

    private T Deserialize()
    {
        if (!_dataOption.IsSome(out var serializedValue))
            throw new InvalidOperationException($"{nameof(Data)} isn't set.");
        var value = serializedValue.Length == 0
            ? default!
            : GetSerializer().Read(serializedValue);
        _valueOption = value;
        return value;
    }

    protected virtual IByteSerializer<T> GetSerializer()
        => ByteSerializer<T>.Default;

    // Equality

    public bool Equals(ByteSerialized<T>? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return StructuralComparisons.StructuralEqualityComparer.Equals(Data, other.Data);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        return Equals((ByteSerialized<T>)obj);
    }

    public override int GetHashCode()
        => Data.GetHashCode();
    public static bool operator ==(ByteSerialized<T>? left, ByteSerialized<T>? right)
        => Equals(left, right);
    public static bool operator !=(ByteSerialized<T>? left, ByteSerialized<T>? right)
        => !Equals(left, right);
}
