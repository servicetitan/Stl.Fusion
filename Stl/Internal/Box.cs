using System;
using System.Text.Json.Serialization;

namespace Stl.Internal
{
    [Serializable]
    public abstract class Box
    {
        [JsonIgnore] public abstract object? UntypedValue { get; }
        public static Box<T> New<T>(T value) => new Box<T>(value);
    }

    [Serializable]
    public class Box<T> : Box
    {
        public T Value { get; set; }
        // ReSharper disable once HeapView.BoxingAllocation
        [JsonIgnore] public override object? UntypedValue => Value;

        public Box(T value = default) => Value = value;
        public override string ToString() => $"{GetType().Name}({Value})";
    }
}
