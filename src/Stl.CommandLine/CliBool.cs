using System;
using System.Globalization;
using Newtonsoft.Json;
using Stl.Internal;

namespace Stl.CommandLine 
{
    [Serializable]
    public readonly struct CliBool : IEquatable<CliBool>, IFormattable
    {
        public bool Value { get; }

        [JsonConstructor]
        public CliBool(bool value) => Value = value;

        public override string ToString() => ToString(null, null);
        public string ToString(string? format, IFormatProvider? provider = null) 
        {
            if (string.IsNullOrEmpty(format)) format = "V";
            provider ??= CultureInfo.InvariantCulture;
            return format.ToUpperInvariant() switch {
                "V" => Value ? "true" : "false",
                "0" => Value ? "1" : "0",
                _ => throw Errors.UnsupportedFormatString(format)
            };
        }

        public bool Equals(CliBool other) => Value.Equals(other.Value);
        public override bool Equals(object? obj) => obj is CliBool other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public static bool operator ==(CliBool left, CliBool right) => left.Equals(right);
        public static bool operator !=(CliBool left, CliBool right) => !left.Equals(right);

        public static implicit operator CliBool(bool source) => new CliBool(source); 
        public static implicit operator bool(CliBool source) => source.Value;
        public static implicit operator CliString(CliBool source) => CliString.New(source.ToString());
    }
}
