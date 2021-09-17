using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Stl.Serialization
{
    [DataContract]
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
    public abstract class Serialized<T> : IEquatable<Serialized<T>>
    {
        private Option<T> _valueOption;
        private Option<string> _dataOption;

        [JsonIgnore, Newtonsoft.Json.JsonIgnore]
        public T Value {
            get => _valueOption.IsSome(out var v) ? v : Deserialize();
            set {
                _valueOption = value;
                _dataOption = Option<string>.None;
            }
        }

        [DataMember(Order = 0)]
        public string Data {
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

        private string Serialize()
        {
            if (!_valueOption.IsSome(out var value))
                throw new InvalidOperationException($"{nameof(Value)} isn't set.");
            var serializedValue = !typeof(T).IsValueType && ReferenceEquals(value, null)
                ? ""
                : GetSerializer().Writer.Write(value);
            _dataOption = serializedValue;
            return serializedValue;
        }

        private T Deserialize()
        {
            if (!_dataOption.IsSome(out var serializedValue))
                throw new InvalidOperationException($"{nameof(Data)} isn't set.");
            var value = string.IsNullOrEmpty(serializedValue)
                ? default!
                : GetSerializer().Reader.Read(serializedValue);
            _valueOption = value;
            return value;
        }

        protected abstract IUtf16Serializer<T> GetSerializer();

        // Equality

        public bool Equals(Serialized<T>? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Data.Equals(other.Data);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            return Equals((Serialized<T>)obj);
        }

        public override int GetHashCode()
            => Data.GetHashCode();
        public static bool operator ==(Serialized<T>? left, Serialized<T>? right)
            => Equals(left, right);
        public static bool operator !=(Serialized<T>? left, Serialized<T>? right)
            => !Equals(left, right);
    }
}
