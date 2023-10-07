namespace Stl.DependencyInjection;

public interface IHasIsDisposed
{
    bool IsDisposed { get; }
}

public interface IHasWhenDisposed : IHasIsDisposed
{
    Task? WhenDisposed { get; }
}
