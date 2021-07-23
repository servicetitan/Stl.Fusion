using System;
using System.Text.Json.Serialization;

namespace Stl.Serialization
{
    public abstract class Serialized<T>
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

        public string Data {
            get => _dataOption.IsSome(out var v) ? v : Serialize();
            set {
                _valueOption = Option<T>.None;
                _dataOption = value;
            }
        }

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
    }
}
