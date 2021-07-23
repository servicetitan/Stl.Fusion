using System.Linq;
using FluentAssertions;
using Stl.Testing;
using Stl.Text;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Text
{
    public class SymbolListTest : TestBase
    {
        public SymbolListTest(ITestOutputHelper @out) : base(@out) { }

        [Theory]
        [InlineData("", new[] {""}, null)]
        [InlineData("\\", new[] {"\\"}, "\\\\")]
        [InlineData("\\[\\]", new [] {""}, "")]
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
            var l = SymbolList.Parse(value);
            l.SegmentCount.Should().Be(segments.Length);
            l.GetSegments().Should().BeEquivalentTo(segments.Select(s => (Symbol) s));
            l.FormattedValue.Should().Be(expectedValue ?? value);
            l.AssertPassesThroughAllSerializers();
        }
    }
}
