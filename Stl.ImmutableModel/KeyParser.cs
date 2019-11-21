using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text;
using Stl.Text;

namespace Stl.ImmutableModel 
{
    public abstract class KeyBase : IEquatable<KeyBase>, ISerializable
    {
        protected static Symbol SerializedValueMemberName = "@";
        protected int CachedHashCode { get; private set; }
        
        public static KeyBase Parse(string formattedValue)
            => KeyParser.Instance.Parse(formattedValue);

        public override string ToString() => $"{GetType().Name}({Format()})";

        public string Format()
        {
            var output = new StringBuilder();
            FormatTo(output);
            return output.ToString();
        }

        public abstract void FormatTo(StringBuilder output);

        // Equality

        public abstract bool Equals(KeyBase other);
        public override int GetHashCode() => CachedHashCode;
        public static bool operator ==(KeyBase left, KeyBase right) => left?.Equals(right) ?? ReferenceEquals(right, null);
        public static bool operator !=(KeyBase left, KeyBase right) => !(left?.Equals(right) ?? ReferenceEquals(right, null));

        // Serialization

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) 
            => info.AddValue(SerializedValueMemberName, Format());
    }

    public class KeyParser
    {
        public static KeyParser Instance { get; }

        public KeyBase Parse(string formattedValue)
            => Parse(formattedValue.AsSpan());
        
        public KeyBase Parse(in ReadOnlySpan<char> formattedValue)
        {
            throw new NotImplementedException();
        }

    }
}
