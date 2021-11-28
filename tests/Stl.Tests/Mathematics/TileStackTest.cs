using Stl.Mathematics;

namespace Stl.Tests.Mathematics;

public class TileStackTest : TestBase
{
    public TileStack<long> LongTileStack = new(0L, 16L, 16_384L, 4);
    public TileStack<Moment> MomentTileStack = new(
        new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        new Moment(TimeSpan.FromMinutes(3)),
        new Moment(TimeSpan.FromMinutes(3 * Math.Pow(4, 10))), // ~ almost 6 years
        4);

    public TileStackTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public void LongTileStackTest()
    {
        var s = LongTileStack;
        s.MinTileSize.Should().Be(16);
        s.MaxTileSize.Should().Be(16_384L);
        var layer0 = s.FirstLayer;
        var layer1 = s.Layers[1];
        var layerL = s.LastLayer;
        layer0.TileSize.Should().Be(s.MinTileSize);
        layer0.TileSizeMultiplier.Should().Be(1);
        layerL.TileSize.Should().Be(s.MaxTileSize);
        s.Layers.Skip(1).Select(l => l.TileSizeMultiplier).All(m => m == 4).Should().BeTrue();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => s.GetTile((0, 17)));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => s.GetTile((1, 17)));

        var tile = layer0.GetTile(-16);
        tile.Should().Be(new Tile<long>(-16, 0, layer0));
        tile.Next().Should().Be(new Tile<long>(0, 16, layer0));
        tile.Next(2).Should().Be(new Tile<long>(16, 32, layer0));
        tile.Prev().Should().Be(new Tile<long>(-32, -16, layer0));
        tile.Prev(2).Should().Be(new Tile<long>(-48, -32, layer0));
        tile.Larger()!.Value.Should().Be(new Tile<long>(-64, 0, layer1));
        tile.Smaller().Length.Should().Be(0);

        layer0.GetTile(1).Start.Should().Be(0);
        layer0.GetTile(17).Start.Should().Be(16);

        tile = layer1.GetTile(16);
        tile.Should().Be(new Tile<long>(0, 64, layer1));
        tile.Next().Should().Be(new Tile<long>(64, 128, layer1));
        tile.Prev().Should().Be(new Tile<long>(-64, 0, layer1));
        tile.Larger()!.Value.Should().Be(new Tile<long>(0, 256, s.Layers[2]));
        tile.Smaller().Select(t => t.Range)
            .Should()
            .BeEquivalentTo(new Range<long>[] {
                (0, 16),
                (16, 32),
                (32, 48),
                (48, 64),
            });
        tile.Smaller().First().Layer.Should().Be(layer0);

        layer1.GetTile(257).Start.Should().Be(256);

        s.TryGetSmallestCoveringTile((-16, 0), out tile).Should().BeTrue();
        tile.Should().Be(new Tile<long>(-16, 0, layer0));

        s.TryGetSmallestCoveringTile((-17, 0), out tile).Should().BeTrue();
        tile.Should().Be(new Tile<long>(-64, 0, layer1));

        layer0.GetCoveringTiles((-1, 1)).Select(t => t.Range)
            .Should()
            .BeEquivalentTo(new Range<long>[] {
                (-16, 0),
                (0, 16),
            });
        layer1.GetCoveringTiles((-65, 1)).Select(t => t.Range)
            .Should()
            .BeEquivalentTo(new Range<long>[] {
                (-128, -64),
                (-64, 0),
                (0, 64),
            });

        s.GetOptimalCoveringTiles((-17, 257)).Select(t => t.Range)
            .Should()
            .BeEquivalentTo(new Range<long>[] {
                (-32, -16),
                (-16, 0),
                (0, 256),
                (256, 256 + 16),
            });

        s.GetOptimalCoveringTiles((-65, 257)).Select(t => t.Range)
            .Should()
            .BeEquivalentTo(new Range<long>[] {
                (-80, -64),
                (-64, 0),
                (0, 256),
                (256, 256 + 16),
            });
    }

    [Fact]
    public void MomentTileStackTest()
    {
        var s = MomentTileStack;
        s.MinTileSize.Should().Be(new Moment(TimeSpan.FromMinutes(3)));
        s.MaxTileSize.Should().Be(new Moment(TimeSpan.FromMinutes(3 * Math.Pow(4, 10))));
        var layer0 = s.FirstLayer;
        var layerL = s.LastLayer;
        layer0.TileSize.Should().Be(s.MinTileSize);
        layer0.TileSizeMultiplier.Should().Be(1);
        layerL.TileSize.Should().Be(s.MaxTileSize);
        s.Layers.Skip(1).Select(l => l.TileSizeMultiplier).All(m => m == 4).Should().BeTrue();

        layer0.GetTile(s.Zero - TimeSpan.FromMinutes(1)).Start.Should().Be(s.Zero - TimeSpan.FromMinutes(3));
        layer0.GetTile(s.Zero + TimeSpan.FromMinutes(1)).Start.Should().Be(s.Zero);
        layer0.GetTile(s.Zero + TimeSpan.FromMinutes(4)).Start.Should().Be(s.Zero + TimeSpan.FromMinutes(3));

        var layer1 = s.Layers[1];
        layer1.GetTile(s.Zero + TimeSpan.FromMinutes(4)).Start.Should().Be(s.Zero + TimeSpan.FromMinutes(0));
        layer1.GetTile(s.Zero + TimeSpan.FromMinutes(25)).Start.Should().Be(s.Zero + TimeSpan.FromMinutes(24));

        s.TryGetSmallestCoveringTile((s.Zero - TimeSpan.FromMinutes(1), s.Zero), out var tile).Should().BeTrue();
        tile.Should().Be(new Tile<Moment>(s.Zero - TimeSpan.FromMinutes(3), s.Zero, layer0));
    }

    [Fact]
    public void MomentTileStackPointTest()
    {
        var c = MomentTileStack;
        var allTiles = c.GetAllTiles(new Moment(16348332675742660));
        foreach (var tile in allTiles)
            c.GetTile(tile.Range).Should().Be(tile);
    }

    [Fact]
    public void SimpleTileStackTest()
    {
        var s = new TileStack<long>(0, 10);
        s.Layers.Length.Should().Be(1);
        s.MaxTileSize.Should().Be(10);
        s.MaxTileSize.Should().Be(10);
    }

    [Fact]
    public void RandomTileCoverTest()
    {
        var s = LongTileStack;
        var rnd = new Random(12);
        for (var i = 0; i < 10_000; i++) {
            var range = new Range<long>(rnd.Next(20_000), rnd.Next(20_000)).Normalize();
            // var range = new Range<long>(91, 1247).Normalize();
            var tiles = s.GetOptimalCoveringTiles(range);
            var union = tiles.First().Range;
            foreach (var tile in tiles.Skip(1)) {
                union.End.Should().Be(tile.Start);
                union = (union.Start, tile.End);
            }

            var startGap = range.Start - union.Start;
            startGap.Should().BeGreaterOrEqualTo(0);
            startGap.Should().BeLessThan(s.MinTileSize);

            var endGap = union.End - range.End;
            endGap.Should().BeGreaterOrEqualTo(0);
            endGap.Should().BeLessThan(s.MinTileSize);
        }
    }
}
