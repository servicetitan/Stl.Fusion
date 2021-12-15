using System.Text.Json.Serialization;
using Stl.Serialization.Internal;

namespace Stl.Serialization;

[DataContract]
public readonly struct ExceptionInfo : IEquatable<ExceptionInfo>
{
    private static readonly Type[] ExceptionCtorArgumentTypes1 = { typeof(string), typeof(Exception) };
    private static readonly Type[] ExceptionCtorArgumentTypes2 = { typeof(string) };

    public static ExceptionInfo None { get; } = default;
    public static Func<ExceptionInfo, Exception?> ToExceptionConverter { get; set; } = DefaultToExceptionConverter;

    private readonly string _message;

    [DataMember(Order = 0)]
    public TypeRef TypeRef { get; }
    [DataMember(Order = 1)]
    public string Message => _message ?? "";
    [IgnoreDataMember]
    public bool IsNone => TypeRef.AssemblyQualifiedName.IsEmpty;

    [JsonConstructor, Newtonsoft.Json.JsonConstructor]
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
            TypeRef = exception.GetType();
            _message = exception.Message;
        }
    }

    public void Deconstruct(out TypeRef typeRef, out string message)
    {
        typeRef = TypeRef;
        message = Message;
    }

    public override string ToString()
        => IsNone
            ? $"{GetType().Name}()"
            : $"{GetType().Name}({TypeRef}, {JsonFormatter.Format(Message)})";

    public Exception? ToException()
    {
        if (IsNone) return null;

        try {
            return ToExceptionConverter.Invoke(this)
                ?? Errors.RemoteException(this);
        }
        catch (Exception) {
            return Errors.RemoteException(this);
        }
    }

    // Conversion

    public static implicit operator ExceptionInfo(Exception exception)
        => new(exception);

    private static Exception? DefaultToExceptionConverter(ExceptionInfo exceptionInfo)
    {
        var type = exceptionInfo.TypeRef.Resolve();
        if (!typeof(Exception).IsAssignableFrom(type))
            return null;

        var ctor = type.GetConstructor(ExceptionCtorArgumentTypes1);
        if (ctor != null) {
            try {
                return (Exception) type.CreateInstance(exceptionInfo.Message, (Exception?) null);
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

        return (Exception) type.CreateInstance(exceptionInfo.Message);
    }

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
}
