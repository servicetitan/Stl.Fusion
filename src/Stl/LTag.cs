using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Stl.Internal;
using Stl.Mathematics;

namespace Stl
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LTag(long value) => Value = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator LTag(long value) => new LTag(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator long(LTag value) => value.Value;

        public override string ToString()
        {
            unsafe {
                Span<char> buffer = stackalloc char[16];
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
            return true;
        }

        // Equality

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(LTag other) => Value == other.Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj) => obj is LTag other && Equals(other);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => Value.GetHashCode();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(LTag left, LTag right) => left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(LTag left, LTag right) => !left.Equals(right);
    }
}
