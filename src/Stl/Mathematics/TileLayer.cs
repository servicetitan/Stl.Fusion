using Stl.Mathematics.Internal;

namespace Stl.Mathematics;

public class TileLayer<T>
    where T : notnull
{
    private readonly Lazy<TileLayer<T>?> _largerLazy;
    private readonly Lazy<TileLayer<T>?> _smallerLazy;

    public int Index { get; init; }
    public T Zero { get; init; } = default!;
    public T TileSize { get; init; } = default!;
    public int TileSizeMultiplier { get; init; } = 1;
    public TileLayer<T>? Larger => _largerLazy.Value;
    public TileLayer<T>? Smaller => _smallerLazy.Value;
    public TileStack<T> Stack { get; init; } = default!;
    public Arithmetics<T> Arithmetics { get; init; } = Arithmetics<T>.Default;

    public TileLayer()
    {
        _largerLazy = new Lazy<TileLayer<T>?>(() => Index + 1 >= Stack.Layers.Length ? null : Stack.Layers[Index + 1]);
        _smallerLazy = new Lazy<TileLayer<T>?>(() => Index <= 0 ? null : Stack.Layers[Index - 1]);
    }

    public bool TryGetTile(Range<T> range, out Tile<T> tile)
    {
        var a = Arithmetics;
        var size = a.Subtract(range.End, range.Start);
        if (EqualityComparer<T>.Default.Equals(size, TileSize)) {
            var mod = a.Mod(a.Subtract(range.Start, Zero), TileSize);
            if (EqualityComparer<T>.Default.Equals(mod, default!)) {
                tile = new(range, this);
                return true;
            }
        }
        tile = default;
        return false;
    }

    public Tile<T> GetTile(Range<T> range)
        => TryGetTile(range, out var tile)
            ? tile
            : throw Errors.InvalidTileBoundaries(nameof(range));

    public Tile<T> GetTile(T point)
    {
        var a = Arithmetics;
        var tileIndex = a.DivNonNegativeRem(a.Subtract(point, Zero), TileSize, out _);
        var start = a.MulAdd(TileSize, tileIndex, Zero);
        var end = a.Add(start, TileSize);
        return new(start, end, this);
    }

    public bool IsTile(Range<T> range)
        => TryGetTile(range, out _);

    public void AssertIsTile(Range<T> range)
    {
        if (!TryGetTile(range, out _))
            throw Errors.InvalidTileBoundaries(nameof(range));
    }

    public Tile<T>[] GetCoveringTiles(Range<T> range)
    {
        var tiles = ArrayBuffer<Tile<T>>.Lease(false);
        using var _ = tiles;
        GetCoveringTiles(range, ref tiles);
        return tiles.ToArray();
    }

    public Tile<T>[] GetOptimalCoveringTiles(Range<T> range)
    {
        var tiles = ArrayBuffer<Tile<T>>.Lease(false);
        using var _ = tiles;
        GetOptimalCoveringTiles(range, ref tiles);
        return tiles.ToArray();
    }

    // Private methods

    private void GetCoveringTiles(Range<T> range, ref ArrayBuffer<Tile<T>> appendTo)
    {
        var c = Comparer<T>.Default;
        var tile = GetTile(range.Start);
        appendTo.Add(tile);
        while (c.Compare(tile.End, range.End) < 0) {
            tile = tile.Next();
            appendTo.Add(tile);
        }
    }

    private void GetOptimalCoveringTiles(Range<T> range, ref ArrayBuffer<Tile<T>> appendTo)
    {
        if (Smaller == null) {
            GetCoveringTiles(range, ref appendTo);
            return;
        }

        var tiles = ArrayBuffer<Tile<T>>.Lease(false);
        using var _ = tiles;
        GetCoveringTiles(range, ref tiles);

        if (tiles.Count == 1) {
            var tile = tiles[0];
            if (tile.IsLeftSubdivisionUseful(range.Start) || tile.IsRightSubdivisionUseful(range.End))
                Smaller.GetOptimalCoveringTiles(range, ref appendTo);
            else
                appendTo.Add(tile);
        }
        else {
            var midTiles = tiles.Span;
            var firstTile = tiles[0];
            var lastTile = tiles[^1];
            if (firstTile.IsLeftSubdivisionUseful(range.Start)) {
                // Left side can be subdivided
                var leftRange = new Range<T>(range.Start, firstTile.End);
                Smaller.GetOptimalCoveringTiles(leftRange, ref appendTo);
                midTiles = midTiles[1..];
            }
            if (lastTile.IsRightSubdivisionUseful(range.End)) {
                // Right side can be subdivided
                midTiles = midTiles[..^1];
                appendTo.AddRange(midTiles);
                var rightRange = new Range<T>(lastTile.Start, range.End);
                Smaller.GetOptimalCoveringTiles(rightRange, ref appendTo);
            }
            else
                appendTo.AddRange(midTiles);
        }
    }
}
