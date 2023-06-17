using MemoryPack;
using Stl.Mathematics.Internal;

namespace Stl.Mathematics;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
[Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
[StructLayout(LayoutKind.Auto)]
public readonly partial struct Tile<T>
    where T : notnull
{
    [DataMember(Order = 0), MemoryPackOrder(0)]
    public Range<T> Range { get; }
    [JsonIgnore, Newtonsoft.Json.JsonIgnore, MemoryPackIgnore]
    public TileLayer<T> Layer { get; }
    [JsonIgnore, Newtonsoft.Json.JsonIgnore, MemoryPackIgnore]
    public TileStack<T> Stack => Layer.Stack;

    [JsonIgnore, Newtonsoft.Json.JsonIgnore, MemoryPackIgnore]
    public T Start => Range.Start;
    [JsonIgnore, Newtonsoft.Json.JsonIgnore, MemoryPackIgnore]
    public T End => Range.End;

    public Tile(T start, T end, TileLayer<T> layer)
    {
        Range = new Range<T>(start, end);
        Layer = layer;
    }

    public Tile(Range<T> range, TileLayer<T> layer)
    {
        Range = range;
        Layer = layer;
    }

    [Newtonsoft.Json.JsonConstructor, JsonConstructor, MemoryPackConstructor]
    private Tile(Range<T> range)
    {
        Range = range;
        Layer = null!;
    }

    public void Deconstruct(out T start, out T end)
    {
        if (Layer == null)
            throw Errors.UnboundTile();
        start = Range.Start;
        end = Range.End;
    }

    public override string ToString()
    {
        var typeName = GetType().Name;
        return Layer == null
            ? $"{typeName}(unbound)"
            : $"{typeName}({Range.Start}..{Range.End})";
    }

    public Tile<T> Next(int index = 1)
    {
        var a = Layer.Arithmetics;
        var offset = a.Mul(Layer.TileSize, index);
        return new Tile<T>((a.Add(Start, offset), a.Add(End, offset)), Layer);
    }

    public Tile<T> Prev(int index = 1)
        => Next(-index);

    public Tile<T>? Larger()
        => Layer.Larger?.GetTile(Start);

    public Tile<T>[] Smaller()
    {
        var smallerLayer = Layer.Smaller;
        if (smallerLayer == null)
            return Array.Empty<Tile<T>>();

        var a = Layer.Arithmetics;
        var tiles = new Tile<T>[Layer.TileSizeMultiplier];
        var start = Start;
        var end = a.Add(start, smallerLayer.TileSize);
        for (var i = 0; i < Layer.TileSizeMultiplier; i++) {
            tiles[i] = new Tile<T>((start, end), smallerLayer);
            start = end;
            end = a.Add(end, smallerLayer.TileSize);
        }
        return tiles;
    }

    // Private / internal methods

    internal bool IsLeftSubdivisionUseful(T start)
    {
        var bestAltStart = Layer.Arithmetics.Add(Start, Stack.MinTileSize);
        return Comparer<T>.Default.Compare(bestAltStart, start) <= 0;
    }

    internal bool IsRightSubdivisionUseful(T end)
    {
        var bestAltEnd = Layer.Arithmetics.Subtract(End, Stack.MinTileSize);
        return Comparer<T>.Default.Compare(end, bestAltEnd) <= 0;
    }
}
