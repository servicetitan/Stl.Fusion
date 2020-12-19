using System.Linq;
using System.Text;
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

            var parser = ListFormat.Default.CreateParser(format, new StringBuilder());
            var list = parser.ParseAll().ToList();
            list.Count.Should().Be(length);

            var formatter = ListFormat.Default.CreateFormatter(new StringBuilder());
            formatter.Append(list);
            formatter.Output.Should().Be(expectedFormat);
        }
    }
}
