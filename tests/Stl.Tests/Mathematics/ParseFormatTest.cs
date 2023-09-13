using Stl.Generators;
using Stl.Mathematics;

namespace Stl.Tests.Mathematics;

public class ParseFormatTest
{
    [Fact]
    public void BasicTest()
    {
        var binary = "01".AsSpan();
        MathExt.Format(0, binary).Should().Be("0");
        MathExt.Format(1, binary).Should().Be("1");
        MathExt.Format(2, binary).Should().Be("10");
        MathExt.Format(3, 2).Should().Be("11");
        MathExt.Format(4, 2).Should().Be("100");

        MathExt.Format(-1, binary).Should().Be("-1");
        MathExt.Format(-2, binary).Should().Be("-10");
        MathExt.Format(-3, 2).Should().Be("-11");

        MathExt.Format(0ul, binary).Should().Be("0");
        MathExt.Format(1ul, binary).Should().Be("1");
        MathExt.Format(2ul, binary).Should().Be("10");
        MathExt.Format(3ul, 2).Should().Be("11");
        MathExt.Format(4ul, 2).Should().Be("100");
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
        for (var i = 0; i < 1000; i++) {
            foreach (var alphabet in alphabets) {
                var n = rnd.Next();
                var f = MathExt.Format(n, alphabet.AsSpan());
                var p = MathExt.ParseInt64(f.AsSpan(), alphabet.AsSpan());
                p.Should().Be(n);
            }
        }
    }
}
