namespace Stl.Mathematics;

// Just a tagging interface
public interface IArithmetics
{ }

public abstract class Arithmetics<T> : IArithmetics
    where T : notnull
{
    public static Arithmetics<T> Default => ArithmeticsProvider.Default.GetArithmetics<T>();

    public T One { get; protected init; } = default!; // Must be set in .ctor!

    // Abstract methods

    public abstract T Negative(T x);
    public abstract T Add(T a, T b);
    public abstract T Mul(T a, double m);
    public abstract T Mul(T a, long m);
    public abstract long DivRem(T a, T b, out T reminder);

    // Non-abstract methods

    public T Subtract(T a, T b) => Add(a, Negative(b));
    public T MulAdd(T a, double m, T b) => Add(Mul(a, m), b);
    public T MulAdd(T a, long m, T b) => Add(Mul(a, m), b);

    public virtual long DivNonNegativeRem(T a, T b, out T reminder)
    {
        var d = DivRem(a, b, out reminder);
        if (Comparer<T>.Default.Compare(reminder, default!) < 0) {
            d--;
            reminder = Add(reminder, b);
        }
        return d;
    }

    public T Mod(T a, T b)
    {
        _ = DivNonNegativeRem(a, b, out var mod);
        return mod;
    }
}
