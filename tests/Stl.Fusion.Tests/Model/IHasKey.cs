namespace Stl.Fusion.Tests.Model;

public interface IHasKey<out TKey>
    where TKey : notnull
{
    TKey Key { get; }
}
