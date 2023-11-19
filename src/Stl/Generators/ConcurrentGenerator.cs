namespace Stl.Generators;

public abstract class ConcurrentGenerator<T> : Generator<T>
{
    public abstract T Next(int random);
    public override T Next() => Next(Environment.CurrentManagedThreadId);
}
