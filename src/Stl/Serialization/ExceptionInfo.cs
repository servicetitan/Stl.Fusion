using Stl.Serialization.Internal;

namespace Stl.Serialization;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public readonly partial struct ExceptionInfo : IEquatable<ExceptionInfo>
{
    private static readonly Type[] ExceptionCtorArgumentTypes1 = { typeof(string), typeof(Exception) };
    private static readonly Type[] ExceptionCtorArgumentTypes2 = { typeof(string) };

    public static ExceptionInfo None { get; } = default;
    public static Func<ExceptionInfo, Exception?> ToExceptionConverter { get; set; } = DefaultToExceptionConverter;

    private readonly string _message;

    [DataMember(Order = 0), MemoryPackOrder(0)]
    public TypeRef TypeRef { get; }
    [DataMember(Order = 1), MemoryPackOrder(1)]
    public string Message => _message ?? "";
    [DataMember(Order = 2), MemoryPackOrder(2)]
    public TypeRef WrappedTypeRef { get; }
    [IgnoreDataMember, MemoryPackIgnore]
    public bool IsNone => TypeRef.AssemblyQualifiedName.IsEmpty;
    [IgnoreDataMember, MemoryPackIgnore]
    public bool HasWrappedTypeRef => !WrappedTypeRef.AssemblyQualifiedName.IsEmpty;

    [JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
    public ExceptionInfo(TypeRef typeRef, string? message, TypeRef wrappedTypeRef = default)
    {
        TypeRef = typeRef;
        _message = message ?? "";
        WrappedTypeRef = wrappedTypeRef;
    }

    public ExceptionInfo(Exception? exception)
    {
        if (exception == null) {
            TypeRef = default;
            _message = "";
            WrappedTypeRef = default;
        } else if (exception is IWrappedException re) {
            var wrappedException = re.Unwrap();
            TypeRef = exception.GetType();
            _message = wrappedException.Message;
            WrappedTypeRef = wrappedException.GetType();
        } else {
            TypeRef = exception.GetType();
            _message = exception.Message;
            WrappedTypeRef = default;
        }
    }

    public override string ToString()
    {
        if (IsNone)
            return $"{GetType().Name}()";
        if (HasWrappedTypeRef)
            return $"{GetType().Name}({TypeRef} -> {WrappedTypeRef}, {JsonFormatter.Format(Message)})";
        return $"{GetType().Name}({TypeRef}, {JsonFormatter.Format(Message)})";
    }

    public Exception? ToException()
        => ToExceptionConverter.Invoke(this);

    public ExceptionInfo Unwrap()
        => HasWrappedTypeRef ? new ExceptionInfo(WrappedTypeRef, Message) : this;

    // Conversion

    public static implicit operator ExceptionInfo(Exception exception)
        => new(exception);

    public static Exception? DefaultToExceptionConverter(ExceptionInfo exceptionInfo)
    {
        if (exceptionInfo.IsNone)
            return null;

        try {
            return CreateException(exceptionInfo) ?? Errors.RemoteException(exceptionInfo);
        }
        catch (Exception) {
            return Errors.RemoteException(exceptionInfo);
        }
    }

    private static Exception? CreateException(ExceptionInfo exceptionInfo)
    {
        var type = exceptionInfo.TypeRef.Resolve();
        if (!exceptionInfo.HasWrappedTypeRef)
            return CreateStandardException(type, exceptionInfo.Message);

        if (!typeof(IWrappedException).IsAssignableFrom(type))
            return null;
        if (!typeof(Exception).IsAssignableFrom(type))
            return null;

        var wrappedType = exceptionInfo.WrappedTypeRef.Resolve();
        var wrappedException = CreateStandardException(wrappedType, exceptionInfo.Message);
        if (wrappedException == null)
            return null;

        var ctor = type.GetConstructor(ExceptionCtorArgumentTypes1);
        if (ctor == null)
            return null;
        return (Exception) type.CreateInstance(exceptionInfo.Message, wrappedException);
    }

    private static Exception? CreateStandardException(Type type, string message)
    {
        if (!typeof(Exception).IsAssignableFrom(type))
            return null;

        var ctor = type.GetConstructor(ExceptionCtorArgumentTypes1);
        if (ctor != null) {
            try {
                return (Exception) type.CreateInstance(message, (Exception?) null);
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

        return (Exception) type.CreateInstance(message);
    }

    // Equality

    public bool Equals(ExceptionInfo other)
        => TypeRef.Equals(other.TypeRef)
            && WrappedTypeRef.Equals(other.WrappedTypeRef)
            && StringComparer.Ordinal.Equals(Message, other.Message);
    public override bool Equals(object? obj)
        => obj is ExceptionInfo other && Equals(other);
    public override int GetHashCode()
        => HashCode.Combine(TypeRef, WrappedTypeRef, StringComparer.Ordinal.GetHashCode(Message));
    public static bool operator ==(ExceptionInfo left, ExceptionInfo right)
        => left.Equals(right);
    public static bool operator !=(ExceptionInfo left, ExceptionInfo right)
        => !left.Equals(right);
}
