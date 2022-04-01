namespace Stl.Pooling;

public interface IResourceLease<out T> : IDisposable
{
    T Resource { get; }
}
