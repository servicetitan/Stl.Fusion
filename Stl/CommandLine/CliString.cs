using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Stl.Internal;
using Stl.OS;

namespace Stl.CommandLine 
{
    // Helper type to deal with command line parts
    [Serializable]
    public struct CliString : IEquatable<CliString>, IFormattable
    {
        private readonly string? _value;
        
        // Value is never null; the check is done here b/c structs can be constructed w/o calling .ctor
        public string Value => _value ?? "";

        public string QuotedValue => Quote(Value).Value;
        public CliString(string? value) => _value = value;
        
        public override string ToString() => Value;
        public string ToString(string format, IFormatProvider? provider = null) 
        {
            if (string.IsNullOrEmpty(format)) format = "V";
            provider ??= CultureInfo.InvariantCulture;
            return format.ToUpperInvariant() switch {
                "V" => Value,
                "Q" => QuotedValue,
                _ => throw Errors.UnsupportedFormatString(format)
            };
        }

        public CliString Append(CliString tail, char delimiter = ' ') => Concat(this, tail, delimiter); 
        public CliString Append(params CliString[] parts) => Concat(Concat(parts)); 
        public CliString Append(IEnumerable<CliString> parts) => Concat(Concat(parts));

        public CliString Quote() 
            => Quote(Value);
        
        public CliString VaryByOS(CliString? windowsValue, CliString? macOSValue = null)
            => OSInfo.Kind switch {
                OSKind.Windows => windowsValue ?? this,
                OSKind.MacOS => macOSValue ?? this,
                _ => this 
            };

        // Equality

        public bool Equals(CliString other) => string.Equals(Value, other.Value);
        public override bool Equals(object obj) => obj is CliString other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public static bool operator ==(CliString left, CliString right) => left.Equals(right);
        public static bool operator !=(CliString left, CliString right) => !left.Equals(right);

        // Operators
        
        public static implicit operator CliString(string source) 
            => new CliString(source);
        public static CliString operator+(CliString first, CliString? second) 
            => first.Append(second ?? ""); 

        // Static members

        public static CliString Empty { get; } = new CliString("");
        public static CliString New(string value) 
            => new CliString(value ?? "");

        public static CliString UnixQuote(string value) => 
            "'" + value.Replace("'", "'\"'\"'") + "'";
        public static CliString WindowsQuote(string value) => 
            "\"" + value.Replace("^", "^^").Replace("\"", "^\"") + "\"";
        public static CliString Quote(string value) =>
            OSInfo.Kind switch {
                OSKind.Windows => WindowsQuote(value),
                _ => UnixQuote(value)
            };

        public static CliString Concat(CliString head, CliString tail, char delimiter = ' ')
            => string.IsNullOrEmpty(tail.Value)
                ? head
                : string.IsNullOrEmpty(head.Value) ? tail : $"{head}{delimiter}{tail}";
        public static CliString Concat(params CliString[] parts) 
            => Concat((IEnumerable<CliString>) parts);
        public static CliString Concat(IEnumerable<CliString> parts)
        {
            var sb = new StringBuilder();
            var prefix = "";
            foreach (var part in parts) {
                if (string.IsNullOrEmpty(part.Value))
                    continue;
                sb.Append(prefix);
                sb.Append(part.Value);
                prefix = " ";
            }
            return sb.ToString();
        }
    }
}
