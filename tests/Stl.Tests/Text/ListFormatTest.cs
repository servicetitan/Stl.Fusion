namespace Stl.Tests.Text;

public class ListFormatTest : TestBase
{
    public ListFormatTest(ITestOutputHelper @out) : base(@out) { }

    [Theory]
    [InlineData("\\", 0, null)]
    [InlineData("\\\\", 1, null)]
    [InlineData("\\[\\]", 1, "[]")]
    [InlineData("\\[]", 1, "[]")]
    [InlineData("[\\]", 1, "[]")]
    [InlineData("[]", 1, null)]
    [InlineData("", 1, null)]
    [InlineData("a", 1, null)]
    [InlineData("a|b", 2, null)]
    public void BasicTest(string format, int length, string? expectedFormat)
    {
        expectedFormat ??= format;

        using var p = ListFormat.Default.CreateParser(format);
        var list = p.ParseAll().ToList();
        list.Count.Should().Be(length);

        using var f = ListFormat.Default.CreateFormatter();
        f.Append(list);
        f.Output.Should().Be(expectedFormat);
    }

    [Theory]
    [InlineData("", new[] {""}, null)]
    [InlineData("\\", new string[0], null)]
    [InlineData("\\[\\]", new [] {"[]"}, "[]")]
    [InlineData("\\\\[\\\\]", new [] {"\\[\\]"})]
    [InlineData("a", new[] {"a"}, null)]
    [InlineData("a|b", new[] {"a", "b"}, null)]
    [InlineData("a\\|b", new[] {"a|b"}, null)]
    [InlineData("a\\\\|b", new[] {"a\\", "b"}, null)]
    [InlineData("a\\\\|b\\", new[] {"a\\", "b\\"}, "a\\\\|b\\\\")]
    [InlineData("a\\\\|b\\\\", new[] {"a\\", "b\\"}, null)]
    [InlineData("a\\\\", new[] {"a\\"}, null)]
    [InlineData("a\\", new[] {"a\\"}, "a\\\\")]
    [InlineData("a|\\", new[] {"a", "\\"}, "a|\\\\")]
    public void AdvancedTest(string value, string[] segments, string? expectedValue = null)
    {
        var listFormat = ListFormat.Default;
        var l = listFormat.Parse(value, new List<string>());
        l.Count.Should().Be(segments.Length);
        l.Should().BeEquivalentTo(segments);
        var l2 = listFormat.Parse(value);
        l2.Should().BeEquivalentTo(l);
#if NET5_0_OR_GREATER || NETCOREAPP
        l2 = listFormat.Parse((ReadOnlySpan<char>) value, new List<string>());
        l2.Should().BeEquivalentTo(l);
        l2 = listFormat.Parse((ReadOnlySpan<char>) value);
        l2.Should().BeEquivalentTo(l);
#endif

        var f = listFormat.Format(l);
        f.Should().Be(expectedValue ?? value);
        var f2 = listFormat.Format(l.AsEnumerable());
        f2.Should().Be(f);
    }
}
