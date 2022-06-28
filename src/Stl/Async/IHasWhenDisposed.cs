namespace Stl.Async;

public interface IHasWhenDisposed
{
    Task? WhenDisposed { get; }
}
