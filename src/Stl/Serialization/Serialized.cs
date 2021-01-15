using System;
using Newtonsoft.Json;

namespace Stl.Serialization
{
    public abstract class Serialized<TValue>
    {
        private Option<TValue> _valueOption;
        private Option<string> _serializedValueOption;

        [JsonIgnore]
        public TValue Value {
            get => _valueOption.IsSome(out var v) ? v : Deserialize();
            set {
                _valueOption = value;
                _serializedValueOption = Option<string>.None;
            }
        }

        public string SerializedValue {
            get => _serializedValueOption.IsSome(out var v) ? v : Serialize();
            set {
                _valueOption = Option<TValue>.None;
                _serializedValueOption = value;
            }
        }

        private string Serialize()
        {
            if (!_valueOption.IsSome(out var value))
                throw new InvalidOperationException($"{nameof(Value)} isn't set.");
            var serializedValue = !typeof(TValue).IsValueType && ReferenceEquals(value, null)
                ? ""
                : CreateSerializer().Serialize(value);
            _serializedValueOption = serializedValue;
            return serializedValue;
        }

        private TValue Deserialize()
        {
            if (!_serializedValueOption.IsSome(out var serializedValue))
                throw new InvalidOperationException($"{nameof(SerializedValue)} isn't set.");
            var value = string.IsNullOrEmpty(serializedValue)
                ? default!
                : CreateSerializer().Deserialize<TValue>(serializedValue);
            _valueOption = value;
            return value;
        }

        protected abstract ISerializer<string> CreateSerializer();
    }
}
