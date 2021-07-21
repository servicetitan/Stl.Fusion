using System.Linq;
using FluentAssertions;
using Stl.Testing;
using Stl.Text;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Text
{
    public class ListFormatTest : TestBase
    {
        public ListFormatTest(ITestOutputHelper @out) : base(@out) { }

        [Theory]
        [InlineData("\\[\\]", 0, null)]
        [InlineData("\\[]", 1, "[]")]
        [InlineData("[\\]", 1, "[]")]
        [InlineData("[]", 1, null)]
        [InlineData("", 1, null)]
        [InlineData("a", 1, null)]
        [InlineData("a|b", 2, null)]
        public void CombinedTest(string format, int length, string? expectedFormat)
        {
            expectedFormat ??= format;

            using var p = ListFormat.Default.CreateParser(format);
            var list = p.ParseAll().ToList();
            list.Count.Should().Be(length);

            using var f = ListFormat.Default.CreateFormatter();
            f.Append(list);
            f.Output.Should().Be(expectedFormat);
        }
    }
}
