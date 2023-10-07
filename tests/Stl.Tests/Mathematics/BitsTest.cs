namespace Stl.Tests.Mathematics;

public class BitsTest(ITestOutputHelper @out) : TestBase(@out)
{
    [Fact]
    public void BasicTest()
    {
        Bits.PopCount(ulong.MaxValue).Should().Be(64);
        Bits.LeadingBitIndex(0).Should().Be(64);
        Bits.LeadingZeroCount(0).Should().Be(64);
        Bits.TrailingZeroCount(0).Should().Be(64);
        Bits.LeadingBitMask(0).Should().Be(0);
        Bits.TrailingBitMask(0).Should().Be(0);
        for (var i = 0; i < 64; i++) {
            var x = 1ul << i;
            var xl = x | (x >> 1) | (x >> 5);
            var xh = x | (x << 1) | (x << 5);
            Bits.LeadingBitIndex(xl).Should().Be(i);
            Bits.LeadingZeroCount(xl).Should().Be(63 - i);
            Bits.TrailingZeroCount(xh).Should().Be(i);
            Bits.LeadingBitMask(xl).Should().Be(x);
            Bits.TrailingBitMask(xh).Should().Be(x);
            Bits.IsPowerOf2(x).Should().BeTrue();
            Bits.IsPowerOf2(xh).Should().Be(x == xh);
            Bits.IsPowerOf2(xl).Should().Be(x == xl);
        }
    }
}
