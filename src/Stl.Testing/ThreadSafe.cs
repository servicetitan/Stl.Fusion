namespace Stl.Testing;

public class ThreadSafe<T>
{
    private readonly object _lock = new();
    private T _value;

    public T Value {
        get {
            lock (_lock) {
                return _value;
            }
        }
        set {
            lock (_lock) {
                _value = value;
            }
        }
    }

    public ThreadSafe() : this(default!) { }
#pragma warning disable CS8618
    public ThreadSafe(T value) => Value = value!;
#pragma warning restore CS8618

    public override string ToString() => $"{GetType().GetName()}({Value})";

    public static implicit operator ThreadSafe<T>(T value) => new(value);
    public static implicit operator T(ThreadSafe<T> value) => value.Value;
}

public static class ThreadSafe
{
    public static ThreadSafe<T> New<T>(T value) => new(value);
}
