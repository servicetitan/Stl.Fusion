using System.ComponentModel;
using System.Text.Json.Serialization;
using Stl.Internal;
using Stl.IO.Internal;

namespace Stl.IO;

[DataContract]
[JsonConverter(typeof(FilePathJsonConverter))]
[Newtonsoft.Json.JsonConverter(typeof(FilePathNewtonsoftJsonConverter))]
[TypeConverter(typeof(FilePathTypeConverter))]
public readonly partial struct FilePath : IEquatable<FilePath>, IComparable<FilePath>
{
    public static readonly FilePath Empty = new("");

    private readonly string? _value;

    [DataMember(Order = 0)]
    public string Value => _value ?? "";
    public int Length => Value.Length;

    public FilePath(string? value) => _value = value;
    public static FilePath New(string? value) => new(value ?? "");

    // Conversion
    public override string ToString() => Value;
    public static implicit operator FilePath(string? source) => new(source);
    public static implicit operator string(FilePath source) => source.Value;

    // Operators
    public static FilePath operator +(FilePath p1, FilePath p2)
        => p1.Value + p2.Value;
    public static FilePath operator |(FilePath p1, FilePath p2)
        => JoinOrTakeSecond(p1.Value, p2.Value);
    public static FilePath operator &(FilePath p1, FilePath p2)
        => Join(p1.Value, p2.Value);

    // Equality
    public bool Equals(FilePath other) => StringComparer.Ordinal.Equals(Value, other.Value);
    public override bool Equals(object? obj) => obj is FilePath other && Equals(other);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public static bool operator ==(FilePath left, FilePath right) => left.Equals(right);
    public static bool operator !=(FilePath left, FilePath right) => !left.Equals(right);

    // Comparison
    public int CompareTo(FilePath other)
        => string.Compare(Value, other.Value, StringComparison.Ordinal);

    // Useful helpers
    public bool IsEmpty => string.IsNullOrEmpty(_value);
#if !NETSTANDARD2_0
    public bool IsFullyQualified => Path.IsPathFullyQualified(Value);
#else
    public bool IsFullyQualified => PathCompatExt.IsPathFullyQualified(Value);
#endif
    public bool IsRooted => Path.IsPathRooted(Value);
    public bool HasExtension => Path.HasExtension(Value);
    public string Extension => Path.GetExtension(Value);
    public FilePath DirectoryPath => Path.GetDirectoryName(Value);
    public FilePath FileName => Path.GetFileName(Value);
    public FilePath FileNameWithoutExtension => Path.GetFileNameWithoutExtension(Value);
    public FilePath FullPath => Path.GetFullPath(Value);

    public FilePath ChangeExtension(string newExtension) => Path.ChangeExtension(Value, newExtension);
#if !NETSTANDARD2_0
    public FilePath RelativeTo(FilePath relativeTo) => Path.GetRelativePath(relativeTo, Value);
#else
    public FilePath RelativeTo(FilePath relativeTo) => PathCompatExt.GetRelativePath(relativeTo, Value);
#endif

    public FilePath Normalize() => Value
        .Replace('\\', Path.DirectorySeparatorChar)
        .Replace('/', Path.DirectorySeparatorChar);

    public FilePath ToAbsolute(FilePath? basePath = null)
    {
        if (basePath != null)
#if !NETSTANDARD2_0
            return Path.GetFullPath(Value, basePath.Value);
#else
            return PathCompatExt.GetFullPath(Value, basePath.Value);
#endif
        if (!IsFullyQualified)
            throw Errors.PathIsRelative(null);
        return Path.GetFullPath(Value);
    }

    public static FilePath JoinOrTakeSecond(string s1, string s2)
        => Path.Combine(s1, s2);

    public static FilePath Join(string s1, string s2)
#if !NETSTANDARD2_0
        => string.IsNullOrEmpty(s2)
            ? s1
            : Path.IsPathFullyQualified(s2)
                ? throw new ArgumentOutOfRangeException(s2)
                : Path.Join(s1, s2);
#else
        => string.IsNullOrEmpty(s2)
            ? s1
            : PathCompatExt.IsPathFullyQualified(s2)
                ? throw new ArgumentOutOfRangeException(s2)
                : PathCompatExt.Join(s1, s2);
#endif
}
