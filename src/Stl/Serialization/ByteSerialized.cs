using System;
using System.Collections;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Microsoft.Toolkit.HighPerformance.Buffers;

namespace Stl.Serialization
{
    [DataContract]
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
    public abstract class ByteSerialized<T> : IEquatable<ByteSerialized<T>>
    {
        private Option<T> _valueOption;
        private Option<byte[]> _dataOption;

        [JsonIgnore, Newtonsoft.Json.JsonIgnore]
        public T Value {
            get => _valueOption.IsSome(out var v) ? v : Deserialize();
            set {
                _valueOption = value;
                _dataOption = Option<byte[]>.None;
            }
        }

        [DataMember(Order = 0)]
        public byte[] Data {
            get => _dataOption.IsSome(out var v) ? v : Serialize();
            set {
                _valueOption = Option<T>.None;
                _dataOption = value;
            }
        }

        // ToString

        public override string ToString()
            => $"{GetType().Name} {{ Data = {SystemJsonSerializer.Default.Write(Data)} }}";

        // Private & protected methods

        private byte[] Serialize()
        {
            if (!_valueOption.IsSome(out var value))
                throw new InvalidOperationException($"{nameof(Value)} isn't set.");
            byte[] serializedValue;
            if (!typeof(T).IsValueType && ReferenceEquals(value, null)) {
                serializedValue = Array.Empty<byte>();
            } else {
                using var bufferWriter = GetSerializer().Writer.Write(value);
                serializedValue = bufferWriter.WrittenSpan.ToArray();
            }
            _dataOption = serializedValue;
            return serializedValue;
        }

        private T Deserialize()
        {
            if (!_dataOption.IsSome(out var serializedValue))
                throw new InvalidOperationException($"{nameof(Data)} isn't set.");
            var value = serializedValue.Length == 0
                ? default!
                : GetSerializer().Reader.Read(serializedValue);
            _valueOption = value;
            return value;
        }

        protected abstract IByteSerializer<T> GetSerializer();

        // Equality

        public bool Equals(ByteSerialized<T>? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return StructuralComparisons.StructuralEqualityComparer.Equals(Data, other.Data);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            return Equals((ByteSerialized<T>)obj);
        }

        public override int GetHashCode()
            => Data.GetHashCode();
        public static bool operator ==(ByteSerialized<T>? left, ByteSerialized<T>? right)
            => Equals(left, right);
        public static bool operator !=(ByteSerialized<T>? left, ByteSerialized<T>? right)
            => !Equals(left, right);
    }
}
