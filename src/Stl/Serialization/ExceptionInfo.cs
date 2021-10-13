using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Stl.Internal;
using Stl.Reflection;
using Stl.Text;

namespace Stl.Serialization
{
    [DataContract]
    public readonly struct ExceptionInfo
    {
        public static readonly ExceptionInfo None = default;
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
        {
            return TypeRef.AssemblyQualifiedName.IsEmpty
                ? $"{GetType().Name}()"
                : $"{GetType().Name}({TypeRef}, {JsonFormatter.Format(Message)})";
        }

        public Exception? ToException()
        {
            if (IsNone) return null;
            var type = TypeRef.Resolve();
            if (!typeof(Exception).IsAssignableFrom(type))
                throw Errors.WrongExceptionType(type);
            return (Exception) type.CreateInstance(Message);
        }

        // Conversion

        public static implicit operator ExceptionInfo(Exception exception)
            => new(exception);

        // Equality

        public bool Equals(ExceptionInfo other)
            => TypeRef.Equals(other.TypeRef) && Message == other.Message;
        public override bool Equals(object? obj)
            => obj is ExceptionInfo other && Equals(other);
        public override int GetHashCode()
            => HashCode.Combine(TypeRef, Message);
        public static bool operator ==(ExceptionInfo left, ExceptionInfo right)
            => left.Equals(right);
        public static bool operator !=(ExceptionInfo left, ExceptionInfo right)
            => !left.Equals(right);
    }
}
