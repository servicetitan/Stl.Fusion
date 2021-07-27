using System;
using System.Text.Json.Serialization;

namespace Stl.Testing
{
    [Serializable]
    public class Box<T>
    {
        public T Value { get; set; }

        public Box() => Value = default!;
        [JsonConstructor, Newtonsoft.Json.JsonConstructor]
        public Box(T value) => Value = value!;
        public override string ToString() => $"{GetType().Name}({Value})";
    }

    public static class Box
    {
        public static Box<T> New<T>(T value) => new Box<T>(value);
    }
}
