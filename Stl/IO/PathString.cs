using System;
using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;
using Stl.Internal;

namespace Stl.IO
{
    [Serializable]
    [JsonConverter(typeof(PathStringJsonConverter))]
    [TypeConverter(typeof(PathStringTypeConverter))]
    public readonly struct PathString : IEquatable<PathString>, IComparable<PathString>
    {
        public static readonly PathString Empty = new PathString("");

        private readonly string? _value;

        public string Value => _value ?? "";

        [JsonConstructor]
        public PathString(string? value) => _value = value;
        public static PathString New(string? value) => new PathString(value ?? "");

        // Conversion
        public override string ToString() => Value;
        public static implicit operator PathString(string? source) => new PathString(source);
        public static implicit operator string(PathString source) => source.Value;

        // Operators
        public static PathString operator +(PathString p1, PathString p2) 
            => p1.Value + p2.Value;
        public static PathString operator |(PathString p1, PathString p2) 
            => JoinOrTakeSecond(p1.Value, p2.Value);
        public static PathString operator &(PathString p1, PathString p2) 
            => Join(p1.Value, p2.Value);

        // Equality
        public bool Equals(PathString other) => Value == other.Value;
        public override bool Equals(object? obj) => obj is PathString other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public static bool operator ==(PathString left, PathString right) => left.Equals(right);
        public static bool operator !=(PathString left, PathString right) => !left.Equals(right);

        // Comparison
        public int CompareTo(PathString other) 
            => string.Compare(Value, other.Value, StringComparison.Ordinal);

        // Useful helpers
        public bool IsEmpty() => string.IsNullOrEmpty(_value);
        public bool IsFullyQualified() => Path.IsPathFullyQualified(Value);
        public bool IsRooted() => Path.IsPathRooted(Value); 
        public bool HasExtension() => Path.HasExtension(Value);

        public string GetExtension() => Path.GetExtension(Value);
        public PathString GetDirectoryPath() => Path.GetDirectoryName(Value);
        public PathString GetFileName() => Path.GetFileName(Value);
        public PathString GetFileNameWithoutExtension() => Path.GetFileNameWithoutExtension(Value);
        public PathString GetFullPath() => Path.GetFullPath(Value);

        public PathString ChangeExtension(string newExtension) => Path.ChangeExtension(Value, newExtension);
        public PathString RelativeTo(PathString relativeTo) => Path.GetRelativePath(relativeTo, Value);

        public PathString Normalize() => Value
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);

        public static PathString JoinOrTakeSecond(string s1, string s2) 
            => Path.Combine(s1, s2);
        public static PathString Join(string s1, string s2) 
            => string.IsNullOrEmpty(s2) 
                ? s1 
                : Path.IsPathFullyQualified(s2) 
                    ? throw new ArgumentOutOfRangeException(s2)
                    : Path.Join(s1, s2);
    }
}
