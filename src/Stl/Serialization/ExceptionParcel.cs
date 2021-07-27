using System;
using System.Text.Json.Serialization;
using Stl.Internal;
using Stl.Reflection;

namespace Stl.Serialization
{
    [Serializable]
    public readonly struct ExceptionParcel : IEquatable<ExceptionParcel>
    {
        public TypeRef TypeRef { get; }
        public string Message { get; }

        public ExceptionParcel(Exception? exception)
            : this(exception?.GetType() ?? default(TypeRef), exception?.Message ?? "") { }

        [JsonConstructor, Newtonsoft.Json.JsonConstructor]
        public ExceptionParcel(TypeRef typeRef, string message)
        {
            TypeRef = typeRef;
            Message = message;
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
                : $"{GetType().Name}({TypeRef}, {SystemJsonSerializer.Default.Write(Message)})";
        }

        public Exception? ToException()
        {
            if (TypeRef.AssemblyQualifiedName.IsEmpty)
                return null;
            var type = TypeRef.Resolve();
            if (!typeof(Exception).IsAssignableFrom(type))
                throw Errors.WrongExceptionType(type);
            return (Exception) type.CreateInstance(Message);
        }

        // Conversion

        public static implicit operator ExceptionParcel(Exception exception)
            => new(exception);

        // Equality

        public bool Equals(ExceptionParcel other)
            => TypeRef.Equals(other.TypeRef) && Message == other.Message;
        public override bool Equals(object? obj)
            => obj is ExceptionParcel other && Equals(other);
        public override int GetHashCode()
            => HashCode.Combine(TypeRef, Message);
        public static bool operator ==(ExceptionParcel left, ExceptionParcel right)
            => left.Equals(right);
        public static bool operator !=(ExceptionParcel left, ExceptionParcel right)
            => !left.Equals(right);
    }
}
