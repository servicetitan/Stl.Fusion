using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Stl.Conversion;
using Stl.Serialization.Internal;

namespace Stl.Serialization;

[DataContract]
[JsonConverter(typeof(JsonStringJsonConverter))]
[Newtonsoft.Json.JsonConverter(typeof(JsonStringNewtonsoftJsonConverter))]
[TypeConverter(typeof(JsonStringTypeConverter))]
public class JsonString :
    IEquatable<JsonString>,
    IComparable<JsonString>,
    IConvertibleTo<string?>
{
    public static readonly JsonString? Null = null;
    public static readonly JsonString Empty= new("");

    private readonly string? _value;

    [DataMember(Order = 0)]
    public string Value => _value ?? string.Empty;

    public static JsonString? New(string? value)
        => value == null ? Null : new JsonString(value);

    public JsonString(string value)
        => _value = value;

    public override string ToString()
        => Value;

    // Conversion

    string? IConvertibleTo<string?>.Convert() => Value;

#if !NETSTANDARD2_0
    [return: NotNullIfNotNull("source")]
#endif
    public static implicit operator JsonString?(string? source)
        => New(source);

#if !NETSTANDARD2_0
    [return: NotNullIfNotNull("source")]
#endif
    public static implicit operator string?(JsonString? source)
        => source?.Value;

    // Operators

    public static JsonString operator +(JsonString left, JsonString right) => new(left.Value + right.Value);
    public static JsonString operator +(JsonString left, string? right) => new(left.Value + right);
    public static JsonString operator +(string? left, JsonString right) => new(left + right.Value);

    // Equality & comparison

    public bool Equals(JsonString? other)
        => !ReferenceEquals(other, null)
            && StringComparer.Ordinal.Equals(Value, other.Value);
    public override bool Equals(object? obj)
        => obj is JsonString other && Equals(other);
    public override int GetHashCode()
        => StringComparer.Ordinal.GetHashCode(Value);
    public int CompareTo(JsonString? other)
        => StringComparer.Ordinal.Compare(Value, other?.Value);
    public static bool operator ==(JsonString left, JsonString right) => left.Equals(right);
    public static bool operator !=(JsonString left, JsonString right) => !left.Equals(right);
}
