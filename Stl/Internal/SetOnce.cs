using System;
using System.Collections.Generic;

namespace Stl.Internal
{
    public struct SetOnce<T> : IEquatable<SetOnce<T>>
    {
        private T _value;

        public T Value {
            get => _value;
            set {
                if (IsReadOnly)
                    throw Errors.ThisValueCanBeSetJustOnce();
                _value = value;
                IsReadOnly = true;
            }
        }

        public bool IsReadOnly { get; private set; }

        public SetOnce(T defaultValue) : this() => _value = defaultValue;
        public override string ToString() => $"{Value}";

        // Equality
        public bool Equals(SetOnce<T> other) => EqualityComparer<T>.Default.Equals(_value, other._value);
        public override bool Equals(object? obj) => obj is SetOnce<T> other && Equals(other);
        public override int GetHashCode() => _value?.GetHashCode() ?? 0;
        public static bool operator ==(SetOnce<T> left, SetOnce<T> right) => left.Equals(right);
        public static bool operator !=(SetOnce<T> left, SetOnce<T> right) => !left.Equals(right);
    }

    public struct SetOnceRef<T> : IEquatable<SetOnceRef<T>>
        where T : class
    {
        private T? _value;

        public T? Value {
            get => _value;
            set {
                if (IsReadOnly)
                    throw Errors.ThisValueCanBeSetJustOnce();
                _value = value;
            }
        }

        public bool IsReadOnly => _value != null;

        public override string ToString() => $"{Value}";

        // Equality
        public bool Equals(SetOnceRef<T> other) => _value == other._value;
        public override bool Equals(object? obj) => obj is SetOnceRef<T> other && Equals(other);
        public override int GetHashCode() => _value?.GetHashCode() ?? 0;
        public static bool operator ==(SetOnceRef<T> left, SetOnceRef<T> right) => left.Equals(right);
        public static bool operator !=(SetOnceRef<T> left, SetOnceRef<T> right) => !left.Equals(right);
    }
}
