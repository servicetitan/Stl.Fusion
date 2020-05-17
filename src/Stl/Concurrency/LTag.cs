using System;
using System.ComponentModel;
using Newtonsoft.Json;
using Stl.Concurrency.Internal;
using Stl.Mathematics;

namespace Stl.Concurrency
{
    [Serializable]
    [JsonConverter(typeof(LTagJsonConverter))]
    [TypeConverter(typeof(LTagTypeConverter))]
    public readonly struct LTag : IEquatable<LTag>
    {
        public static readonly string Base62Digits = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"; 
        public static readonly LTag Default = default;

        public readonly long Value;
        public bool IsSpecial => Value <= 0;

        public LTag(long value) => Value = value;

        public static implicit operator LTag(long value) => new LTag(value);
        public static explicit operator long(LTag value) => value.Value;

        public override string ToString()
        {
            unsafe {
                Span<char> buffer = stackalloc char[12];
                buffer[0] = '@';
                var n = MathEx.FormatTo(Value, Base62Digits, buffer.Slice(1));
                return new string(buffer.Slice(0, n.Length + 1));
            }
        }

        public static LTag Parse(string formattedLTag) 
            => TryParse(formattedLTag, out var result) 
                ? result 
                : throw new ArgumentOutOfRangeException(nameof(formattedLTag));

        public static bool TryParse(string formattedLTag, out LTag lTag)
        {
            lTag = default;
            if (formattedLTag == null || formattedLTag.Length < 2)
                return false;
            if (formattedLTag[0] != '@')
                return false;
            if (!MathEx.TryParse(formattedLTag.AsSpan().Slice(1), Base62Digits, out var value)) 
                return false;
            lTag = new LTag(value);
            return false;
        }

        // Equality

        public bool Equals(LTag other) => Value == other.Value;
        public override bool Equals(object? obj) => obj is LTag other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public static bool operator ==(LTag left, LTag right) => left.Equals(right);
        public static bool operator !=(LTag left, LTag right) => !left.Equals(right);
    }
}
