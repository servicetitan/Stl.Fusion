using Stl.Mathematics;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Mathematics
{
    public class MathExTest : TestBase
    {
        public MathExTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public void GcdLcmTest()
        {
            Assert.Equal(1, MathEx.Gcd(3, 5));
            Assert.Equal(4, MathEx.Gcd(8, 12));
            
            Assert.Equal(15, MathEx.Lcm(5, 3));
            Assert.Equal(12, MathEx.Lcm(4, 6));
        }

        [Fact]
        public void PowTest()
        {
            int Add(int x, int y) => x + y;
            
            Assert.Equal(0, MathEx.FastPower(3, 0, 0, Add));
            Assert.Equal(3, MathEx.FastPower(3, 1, 0, Add));
            Assert.Equal(6, MathEx.FastPower(3, 2, 0, Add));
            Assert.Equal(9, MathEx.FastPower(3, 3, 0, Add));
            Assert.Equal(12, MathEx.FastPower(3, 4, 0, Add));
        }
        
        [Fact]
        public void FactorialTest()
        {
            Assert.Equal(1, MathEx.Factorial(0));
            Assert.Equal(1, MathEx.Factorial(1));
            Assert.Equal(2, MathEx.Factorial(2));
            Assert.Equal(6, MathEx.Factorial(3));
            Assert.Equal(24, MathEx.Factorial(4));
        }
    }
}
