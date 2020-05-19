namespace Stl
{
    public interface IHasKey<out TKey>
    {
        TKey Key { get; }
    }
}
