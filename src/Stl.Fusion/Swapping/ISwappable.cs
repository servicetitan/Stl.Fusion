namespace Stl.Fusion.Swapping;

public interface ISwappable
{
    ValueTask Swap(CancellationToken cancellationToken = default);
}
