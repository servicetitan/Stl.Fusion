namespace Stl.Comparison;

// Shouldn't be serializable!
public readonly struct Ref<T> : IEquatable<Ref<T>>
    where T: class?
{
    public T Target { get; }

    public Ref(T target) => Target = target;

    public override string ToString()
        => $"{GetType().Name}({Target?.ToString() ?? "␀"})";

    // Equality
    public bool Equals(Ref<T> other)
        => ReferenceEquals(Target, other.Target);
    public override bool Equals(object? obj)
        => obj is Ref<T> other && Equals(other);
    public override int GetHashCode()
        => RuntimeHelpers.GetHashCode(Target!);
    public static bool operator ==(Ref<T> left, Ref<T> right)
        => left.Equals(right);
    public static bool operator !=(Ref<T> left, Ref<T> right)
        => !left.Equals(right);
}

public static class Ref
{
    public static Ref<T> New<T>(T value)
        where T : class?
        => new(value);
}
