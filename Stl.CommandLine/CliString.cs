using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Stl.Internal;
using Stl.IO;
using Stl.OS;

namespace Stl.CommandLine 
{
    // Helper type to deal with command line parts
    [Serializable]
    public readonly struct CliString : IEquatable<CliString>, IFormattable
    {
        private static readonly Regex WhitespaceRe = 
            new Regex("\\s", RegexOptions.Compiled | RegexOptions.Singleline); 

        private readonly string? _value;
        
        // Value is never null; the check is done here b/c structs can be constructed w/o calling .ctor
        public string Value => _value ?? "";
        [JsonIgnore] public string QuotedValue => Quote(Value).Value;
        
        [JsonConstructor]
        public CliString(string? value) => _value = value;
        
        public override string ToString() => Value;
        public string ToString(string? format, IFormatProvider? provider = null) 
        {
            if (string.IsNullOrEmpty(format)) format = "V";
            provider ??= CultureInfo.InvariantCulture;
            return format.ToUpperInvariant() switch {
                "V" => Value,
                "Q" => QuotedValue,
                _ => throw Errors.UnsupportedFormatString(format)
            };
        }

        public CliString Append(CliString tail, string delimiter = " ") => Concat(this, tail, delimiter); 
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
        public override bool Equals(object? obj) => obj is CliString other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public static bool operator ==(CliString left, CliString right) => left.Equals(right);
        public static bool operator !=(CliString left, CliString right) => !left.Equals(right);

        // Operators
        
        public static implicit operator CliString(string source) 
            => new CliString(source);
        public static implicit operator CliString(PathString source) 
            => new CliString(source.Value);
        public static implicit operator PathString(CliString source) 
            => new PathString(source.Value);

        public static CliString operator +(CliString first, CliString second) 
            => first.Append(second);
        public static CliString operator |(CliString first, CliString second) 
            => PathString.JoinOrTakeSecond(first.Value, second.Value); 
        public static CliString operator &(CliString first, CliString second) 
            => PathString.Join(first.Value, second.Value); 

        // Static members

        public static readonly CliString Empty = new CliString("");
        public static CliString New(string value) => new CliString(value ?? "");

        // TODO: Add support for strong quotes (quoting shell substitutions)
        public static CliString UnixQuote(string value)
            => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        public static CliString WindowsQuote(string value) 
            => "\"" + value.Replace("^", "^^").Replace("\"", "^\"") + "\"";
        public static CliString Quote(string value) 
            => OSInfo.Kind switch {
                OSKind.Windows => WindowsQuote(value),
                _ => UnixQuote(value)
            };
        public static CliString QuoteIfNeeded(string value)
        {
            if (WhitespaceRe.IsMatch(value))
                return Quote(value);
            var quoted = Quote(value).Value;
            return quoted.Substring(1, quoted.Length - 2) != value ? quoted : value;
        }
            
        public static CliString Concat(CliString head, CliString tail, string delimiter = " ")
            => string.IsNullOrEmpty(tail.Value)
                ? head
                : string.IsNullOrEmpty(head.Value) ? tail : $"{head}{delimiter}{tail}";
        public static CliString Concat(params CliString[] parts) 
            => Concat((IEnumerable<CliString>) parts);
        public static CliString Concat(IEnumerable<CliString> parts, string delimiter = " ")
        {
            var sb = new StringBuilder();
            var prefix = "";
            foreach (var part in parts) {
                if (string.IsNullOrEmpty(part.Value))
                    continue;
                sb.Append(prefix);
                sb.Append(part.Value);
                prefix = delimiter;
            }
            return sb.ToString();
        }
    }
}
