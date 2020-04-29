namespace Stl.Pooling
{
    public interface IResourceReleaser<in T>
    {
        bool Release(T resource);
    }
}
