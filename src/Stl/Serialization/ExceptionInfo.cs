using Stl.Serialization.Internal;

namespace Stl.Serialization;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public readonly partial struct ExceptionInfo : IEquatable<ExceptionInfo>
{
    private static readonly Type[] ExceptionCtorArgumentTypes1 = { typeof(string), typeof(Exception) };
    private static readonly Type[] ExceptionCtorArgumentTypes2 = { typeof(string) };

    public static readonly ExceptionInfo None = default;
    public static Func<TypeRef, Type>? UnknownExceptionTypeResolver { get; set; } = null;

    private readonly string _message;

    [DataMember(Order = 0), MemoryPackOrder(0)]
    public TypeRef TypeRef { get; }
    [DataMember(Order = 1), MemoryPackOrder(1)]
    public string Message => _message ?? "";
    [JsonIgnore, Newtonsoft.Json.JsonIgnore, IgnoreDataMember, MemoryPackIgnore]
    public bool IsNone => TypeRef.AssemblyQualifiedName.IsEmpty;

    [JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
    public ExceptionInfo(TypeRef typeRef, string? message)
    {
        TypeRef = typeRef;
        _message = message ?? "";
    }

    public ExceptionInfo(Exception? exception)
    {
        if (exception == null) {
            TypeRef = default;
            _message = "";
        } else {
            TypeRef = new TypeRef(exception.GetType()).WithoutAssemblyVersions();
            _message = exception.Message;
        }
    }

    public override string ToString()
        => IsNone
            ? $"{GetType().Name}()"
            : $"{GetType().Name}({TypeRef.ToString()}, {JsonFormatter.Format(Message)})";

    public Exception? ToException()
    {
        if (IsNone)
            return null;

        try {
            return TryCreateException(this) ?? Errors.RemoteException(this);
        }
        catch (Exception) {
            return Errors.RemoteException(this);
        }
    }

    // Conversion

    public static implicit operator ExceptionInfo(Exception exception)
        => new(exception);

    // Equality

    public bool Equals(ExceptionInfo other)
        => TypeRef.Equals(other.TypeRef)
            && StringComparer.Ordinal.Equals(Message, other.Message);
    public override bool Equals(object? obj)
        => obj is ExceptionInfo other && Equals(other);
    public override int GetHashCode()
        => HashCode.Combine(TypeRef, StringComparer.Ordinal.GetHashCode(Message));
    public static bool operator ==(ExceptionInfo left, ExceptionInfo right)
        => left.Equals(right);
    public static bool operator !=(ExceptionInfo left, ExceptionInfo right)
        => !left.Equals(right);

    // Private methods

    private static Exception? TryCreateException(ExceptionInfo exceptionInfo)
    {
        if (exceptionInfo.IsNone)
            return null;

        var (type, message) = (exceptionInfo.TypeRef.TryResolve(), exceptionInfo.Message);
        type ??= UnknownExceptionTypeResolver?.Invoke(exceptionInfo.TypeRef);
        if (type == null || !typeof(Exception).IsAssignableFrom(type))
            return null;

        var ctor = type.GetConstructor(ExceptionCtorArgumentTypes1);
        if (ctor != null) {
            try {
                return (Exception)type.CreateInstance(message, (Exception?) null);
            }
            catch {
                // Intended
            }
        }

        ctor = type.GetConstructor(ExceptionCtorArgumentTypes2);
        if (ctor == null)
            return null;

        var parameter = ctor.GetParameters().SingleOrDefault();
        if (!StringComparer.Ordinal.Equals("message", parameter?.Name ?? ""))
            return null;

        return (Exception)type.CreateInstance(message);
    }
}
