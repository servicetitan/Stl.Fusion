using Stl.Mathematics;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Mathematics
{
    public class BitsTest : TestBase
    {
        public BitsTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public void BasicTest()
        {
            Assert.Equal(64, Bits.Count(ulong.MaxValue));
            Assert.Equal(63, Bits.MsbIndex(0));
            Assert.Equal(63, Bits.LsbIndex(0));
            for (var i = 0; i < 64; i++) {
                var x = 1ul << i;
                var xl = x | (x >> 1) | (x >> 5);
                var xh = x | (x << 1) | (x << 5);
                Assert.Equal(i, Bits.MsbIndex(xl));
                Assert.Equal(i, Bits.LsbIndex(xh));
                Assert.Equal(x, Bits.Msb(xl));
                Assert.Equal(x, Bits.Lsb(xh));
                Assert.True(Bits.IsPowerOf2(x));
                Assert.True(i < 1 || !Bits.IsPowerOf2(xl));
            }
        }
    }
}
