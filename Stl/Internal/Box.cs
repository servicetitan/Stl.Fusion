namespace Stl.Internal
{
    public class Box<T>
    {
        public T Value { get; set; }
        public Box(T value = default) => Value = value;
        public override string ToString() => $"{GetType().Name}({Value})";
    }
}