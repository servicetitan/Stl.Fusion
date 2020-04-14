using System;
using System.Globalization;
using EnumsNET;
using Newtonsoft.Json;
using Stl.Internal;

namespace Stl.CommandLine
{
    [Serializable]
    public readonly struct CliEnum<T> : IEquatable<CliEnum<T>>, IFormattable 
        where T: struct, Enum
    {
        public T Value { get; }

        [JsonConstructor]
        public CliEnum(T value) => Value = value;
        public override string ToString() => ToString(null, null);
        public string ToString(string? format, IFormatProvider? provider = null) 
        {
            if (string.IsNullOrEmpty(format)) format = "D";
            provider ??= CultureInfo.InvariantCulture;
            return format.ToUpperInvariant() switch {
                "V" => Value.AsString(EnumFormat.EnumMemberValue)!,
                "N" => Value.AsString(EnumFormat.Name)!,
                "D" => Value.AsString(EnumFormat.DisplayName, EnumFormat.Name)!,
                "0" => Value.AsString(EnumFormat.DecimalValue)!,
                _ => throw Errors.UnsupportedFormatString(format)
            };
        }

        public bool Equals(CliEnum<T> other) => Value.Equals(other.Value);
        public override bool Equals(object? obj) => obj is CliEnum<T> other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public static bool operator ==(CliEnum<T> left, CliEnum<T> right) => left.Equals(right);
        public static bool operator !=(CliEnum<T> left, CliEnum<T> right) => !left.Equals(right);

        public static implicit operator CliEnum<T>(T source) => new CliEnum<T>(source); 
        public static implicit operator T(CliEnum<T> source) => source.Value; 
        public static implicit operator CliString(CliEnum<T> source) => CliString.New(source.ToString());
    }
}
