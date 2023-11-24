using System.Diagnostics.CodeAnalysis;
using Stl.Internal;

namespace Stl.Serialization;

public static class ByteSerialized
{
    public static ByteSerialized<TValue> New<TValue>() => new();
    public static ByteSerialized<TValue> New<TValue>(TValue value) => new() { Value = value };
    public static ByteSerialized<TValue> New<TValue>(byte[] data) => new(data);
}

#if !NET5_0
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
[Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
public partial class ByteSerialized<T> : IEquatable<ByteSerialized<T>>
{
    private Option<T> _valueOption;
    private Option<byte[]> _dataOption;

    [JsonIgnore, Newtonsoft.Json.JsonIgnore, IgnoreDataMember, MemoryPackIgnore]
    public T Value {
        [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
        get => _valueOption.IsSome(out var v) ? v : Deserialize();
        set {
            _valueOption = value;
            _dataOption = Option<byte[]>.None;
        }
    }

    [DataMember(Order = 0), MemoryPackOrder(0)]
    public byte[] Data {
        [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
        get => _dataOption.IsSome(out var v) ? v : Serialize();
        set {
            _valueOption = Option<T>.None;
            _dataOption = value;
        }
    }

    public ByteSerialized() { }

    [MemoryPackConstructor]
    public ByteSerialized(byte[] data)
        => _dataOption = data;

    // ToString

    public override string ToString()
        => $"{GetType().GetName()}(...)";

    // Private & protected methods

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
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

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    private T Deserialize()
    {
        if (!_dataOption.IsSome(out var serializedValue))
            throw new InvalidOperationException($"{nameof(Data)} isn't set.");

        var value = serializedValue.Length == 0
            ? default!
            : GetSerializer().Read(serializedValue, out _);
        _valueOption = value;
        return value;
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    protected virtual IByteSerializer<T> GetSerializer()
        => ByteSerializer<T>.Default;

    // Equality

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
#pragma warning disable IL2046
    public bool Equals(ByteSerialized<T>? other)
#pragma warning restore IL2046
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return StructuralComparisons.StructuralEqualityComparer.Equals(Data, other.Data);
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
#pragma warning disable IL2046
    public override bool Equals(object? obj)
#pragma warning restore IL2046
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        return Equals((ByteSerialized<T>)obj);
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
#pragma warning disable IL2046
    public override int GetHashCode()
#pragma warning restore IL2046
        => Data.GetHashCode();
    public static bool operator ==(ByteSerialized<T>? left, ByteSerialized<T>? right)
        => Equals(left, right);
    public static bool operator !=(ByteSerialized<T>? left, ByteSerialized<T>? right)
        => !Equals(left, right);
}
