using System.Linq;
using FluentAssertions;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests
{
    public class SymbolListTest : TestBase
    {
        public SymbolListTest(ITestOutputHelper @out) : base(@out) { }

        [Theory]
        [InlineData("a", new[] {"a"}, null)]
        [InlineData("a|b", new[] {"a", "b"}, null)]
        [InlineData("a\\|b", new[] {"a|b"}, null)]
        [InlineData("a\\\\|b", new[] {"a\\", "b"}, null)]
        [InlineData("a\\\\|b\\", new[] {"a\\", "b\\"}, "a\\\\|b\\\\")]
        [InlineData("a\\\\|b\\\\", new[] {"a\\", "b\\"}, null)]
        [InlineData("a\\\\", new[] {"a\\"}, null)]
        [InlineData("a\\", new[] {"a\\"}, "a\\\\")]
        [InlineData("a|\\", new[] {"a", "\\"}, "a|\\\\")]
        public void CombinedTest(string value, string[] segments, string? expectedValue = null)
        {
            var p = SymbolList.Parse(value);
            p.SegmentCount.Should().Be(segments.Length);
            p.GetSegments().Should().BeEquivalentTo(segments.Select(s => (Symbol) s));
            p.FormattedValue.Should().Be(expectedValue ?? value);

            p.PassThroughJsonConvert().Should().Be(p);
            p.PassThroughBinaryFormatter().Should().Be(p);
        }
    }
}
