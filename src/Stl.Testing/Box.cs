namespace Stl.Testing;

public class Box<T>
{
    public T Value { get; set; }

    public Box() => Value = default!;
    public Box(T value) => Value = value!;

    public override string ToString() => $"{GetType().Name}({Value})";
}

public static class Box
{
    public static Box<T> New<T>(T value) => new Box<T>(value);
}
