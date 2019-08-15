namespace Stl.Internal
{
    public abstract class Box
    {
        public abstract object? UntypedValue { get; }
    }

    public class Box<T> : Box
    {
        public T Value { get; set; }
        // ReSharper disable once HeapView.BoxingAllocation
        public override object? UntypedValue => Value;

        public Box(T value = default) => Value = value;
        public override string ToString() => $"{GetType().Name}({Value})";
    }
}
