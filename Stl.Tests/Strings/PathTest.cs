using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using FluentAssertions;
using Newtonsoft.Json;
using Stl.Internal;
using Stl.Strings;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;
using Path = Stl.Strings.Path;

namespace Stl.Tests.Strings
{
    public class PathTest : TestBase
    {
        public PathTest(ITestOutputHelper @out) : base(@out) { }

        [Theory]
        [InlineData("a", new[] {"a"}, null)]
        [InlineData("a/b", new[] {"a", "b"}, null)]
        [InlineData("a\\/b", new[] {"a/b"}, null)]
        [InlineData("a\\\\/b", new[] {"a\\", "b"}, null)]
        [InlineData("a\\\\/b\\", new[] {"a\\", "b\\"}, "a\\\\/b\\\\")]
        [InlineData("a\\\\/b\\\\", new[] {"a\\", "b\\"}, null)]
        [InlineData("a\\\\", new[] {"a\\"}, null)]
        [InlineData("a\\", new[] {"a\\"}, "a\\\\")]
        [InlineData("a/\\", new[] {"a", "\\"}, "a/\\\\")]
        public void CombinedTest(string value, string[] segments, string? expectedValue = null)
        {
            var p = Path.Parse(value);
            p.SegmentCount.Should().Be(segments.Length);
            p.GetSegments().Should().BeEquivalentTo(segments.Select(s => (Symbol) s));
            p.Value.Should().Be(expectedValue ?? value);

            p.PassThroughJsonConvert().Should().Be(p);
            p.PassThroughBinaryFormatter().Should().Be(p);
        }
    }
}