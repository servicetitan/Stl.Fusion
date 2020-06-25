namespace Stl
{
    public interface IHasId<out TId>
    {
        TId Id { get; }
    }
}
