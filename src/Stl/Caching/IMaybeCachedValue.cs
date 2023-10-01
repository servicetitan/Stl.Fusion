namespace Stl.Caching;

public interface IMaybeCachedValue
{
    Task WhenSynchronized { get; }
}
