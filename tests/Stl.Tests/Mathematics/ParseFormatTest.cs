using System.Diagnostics;
using FluentAssertions;
using Stl.Generators;
using Stl.Mathematics;
using Xunit;

namespace Stl.Tests.Mathematics
{
    public class ParseFormatTest
    {
        [Fact]
        public void BasicTest()
        {
            var binary = "01";
            var f = MathExt.Format(0, binary);
            f.Should().Be("0");
            f = MathExt.Format(1, binary);
            f.Should().Be("1");
            f = MathExt.Format(2, binary);
            f.Should().Be("10");
            f = MathExt.Format(3, binary);
            f.Should().Be("11");
            f = MathExt.Format(4, binary);
            f.Should().Be("100");

            f = MathExt.Format(-1, binary);
            f.Should().Be("-1");
            f = MathExt.Format(-2, binary);
            f.Should().Be("-10");
            f = MathExt.Format(-3, binary);
            f.Should().Be("-11");
        }

        [Fact]
        public void RandomTest()
        {
            var alphabets = new [] {
                "01",
                "012",
                "0123",
                RandomStringGenerator.Base16Alphabet,
                RandomStringGenerator.Base32Alphabet,
                RandomStringGenerator.Base64Alphabet,
            };
            var rnd = new RandomInt64Generator();
            for (int i = 0; i < 1000; i++) {
                foreach (var alphabet in alphabets) {
                    var n = rnd.Next();
                    var f = MathExt.Format(n, alphabet);
                    var p = MathExt.Parse(f, alphabet);
                    p.Should().Be(n);
                }
            }
        }
    }
}
