namespace Stl.Mathematics.Internal;

public static class Errors
{
    public static Exception CantFindArithmetics(Type type)
        => new InvalidOperationException($"Can't find arithmetics for type '{type}'.");

    public static Exception UnboundTile()
        => new InvalidOperationException("The Tile isn't bound to a TileSet.");

    public static Exception InvalidTileBoundaries(string paramName)
        => new ArgumentOutOfRangeException(paramName, "Invalid tile boundaries.");
}
