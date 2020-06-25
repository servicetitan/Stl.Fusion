using System;
using Newtonsoft.Json;

namespace Stl.Internal
{
    public interface IBox
    {
        object? Value { get; set; }
    }

    public interface IBox<T> : IBox
    {
        new T Value { get; set; }
    }

    public static class Box
    {
        public static Box<T> New<T>(T value) => new Box<T>(value);
    }

    [Serializable]
    public class Box<T> : IBox<T>
    {
        public T Value { get; set; }

        object? IBox.Value {
            // ReSharper disable once HeapView.BoxingAllocation
            get => Value;
            set => Value = (T) value!;
        }

        [JsonConstructor]
        public Box(T value = default) => Value = value;
        public override string ToString() => $"{GetType().Name}({Value})";
    }
}
