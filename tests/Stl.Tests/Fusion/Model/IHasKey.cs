namespace Stl.Tests.Fusion.Model
{
    public interface IHasKey<out TKey>
        where TKey : notnull
    {
        TKey Key { get; }
    }
}
