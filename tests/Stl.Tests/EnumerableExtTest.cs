namespace Stl.Tests;

public class EnumerableExtTest(ITestOutputHelper @out) : TestBase(@out)
{
    [Fact]
    public void BasicTest()
    {
        var source1 = new [] { "A", "BB", "AA" };
        var source2 = new [] { "A", "AA", "B", "BB" };
        var source3 = Array.Empty<string>();

        source1.DistinctBy(i => i.Length).Should().BeEquivalentTo("A", "BB");
        source2.DistinctBy(i => i.Length).Should().BeEquivalentTo("A", "AA");
        source3.DistinctBy(i => i.Length).Should().BeEquivalentTo();

        source1.Chunk(1).Select(p => p.Length).Should().BeEquivalentTo(new[] {1, 1, 1});
        source1.Chunk(2).Select(p => p.Length).Should().BeEquivalentTo(new[] {2, 1});
        source1.Chunk(3).Select(p => p.Length).Should().BeEquivalentTo(new[] {3});

        source2.Chunk(1).Select(p => p.Length).Should().BeEquivalentTo(new[] {1, 1, 1, 1});
        source2.Chunk(2).Select(p => p.Length).Should().BeEquivalentTo(new[] {2, 2});
        source2.Chunk(3).Select(p => p.Length).Should().BeEquivalentTo(new[] {3, 1});
        source2.Chunk(4).Select(p => p.Length).Should().BeEquivalentTo(new[] {4});

        source3.Chunk(4).Select(p => p.Length).Should().BeEquivalentTo(Array.Empty<int>());
    }

    [Fact]
    public void OrderByDependencyTest()
    {
        IEnumerable<char> DepSelector1(char c)
            => Enumerable
                .Range(0, c - '0')
                .Select(i => (char) ('0' + i));
        IEnumerable<char> BadDepSelector1(char c) => new [] {c};
        IEnumerable<char> BadDepSelector2(char c)
            => Enumerable
                .Range(1, 5)
                .Select(i => (char) ('0' + (c - '0' + i) % 10));


        string OrderByDep(string s, Func<char, IEnumerable<char>> depSelector)
            => s.OrderByDependency(depSelector).ToDelimitedString("");

        Assert.Equal("", OrderByDep("", DepSelector1));
        Assert.Equal("01", OrderByDep("1", DepSelector1));
        Assert.Equal("012", OrderByDep("12", DepSelector1));
        Assert.Equal("012", OrderByDep("21", DepSelector1));
        Assert.Equal("0123", OrderByDep("231", DepSelector1));

        Assert.Throws<InvalidOperationException>(() => {
            _ = OrderByDep("0", BadDepSelector1);
        });
        Assert.Throws<InvalidOperationException>(() => {
            _ = OrderByDep("0", BadDepSelector2);
        });
    }
}
