namespace Stl.Pooling
{
    public interface IPool<T> : IResourceReleaser<T>
    {
        ResourceLease<T> Rent();
    }
}
