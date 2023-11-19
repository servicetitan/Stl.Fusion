using System.Diagnostics.CodeAnalysis;
using Stl.Internal;

namespace Stl.Serialization;

public static class TextSerialized
{
    public static TextSerialized<TValue> New<TValue>() => new();
    public static TextSerialized<TValue> New<TValue>(TValue value) => new() { Value = value };
    public static TextSerialized<TValue> New<TValue>(string data) => new(data);
}

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
[Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
public partial class TextSerialized<T> : IEquatable<TextSerialized<T>>
{
    private Option<T> _valueOption;
    private Option<string> _dataOption;

    [JsonIgnore, Newtonsoft.Json.JsonIgnore, IgnoreDataMember, MemoryPackIgnore]
    public T Value {
        [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
        get => _valueOption.IsSome(out var v) ? v : Deserialize();
        set {
            _valueOption = value;
            _dataOption = Option<string>.None;
        }
    }

    [DataMember(Order = 0), MemoryPackOrder(0)]
    public string Data {
        [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
        get => _dataOption.IsSome(out var v) ? v : Serialize();
        set {
            _valueOption = Option<T>.None;
            _dataOption = value;
        }
    }

    // ToString

    public TextSerialized() { }

    [MemoryPackConstructor]
    public TextSerialized(string data)
        => _dataOption = data;

    public override string ToString()
        => $"{GetType().GetName()}(...)";

    // Private & protected methods

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
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

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
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

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    protected virtual ITextSerializer<T> GetSerializer()
        => TextSerializer<T>.Default;

    // Equality

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
#pragma warning disable IL2046
    public bool Equals(TextSerialized<T>? other)
#pragma warning restore IL2046
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return StringComparer.Ordinal.Equals(Data, other.Data);
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
        return Equals((TextSerialized<T>)obj);
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
#pragma warning disable IL2046
    public override int GetHashCode()
#pragma warning restore IL2046
        => StringComparer.Ordinal.GetHashCode(Data);
    public static bool operator ==(TextSerialized<T>? left, TextSerialized<T>? right)
        => Equals(left, right);
    public static bool operator !=(TextSerialized<T>? left, TextSerialized<T>? right)
        => !Equals(left, right);
}
