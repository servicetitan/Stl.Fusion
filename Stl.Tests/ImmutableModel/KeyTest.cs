using Stl.ImmutableModel;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.ImmutableModel
{
    public class KeyTest : TestBase
    {
        public KeyTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public void KeyComparisonTest()
        {
            var keys = new Key[] {
                Key.Parse("k1"),
                Key.Parse("k1|k2"),
                Key.DefaultRootKey,
                Key.Undefined,
            };

            for (var i = 0; i < keys.Length; i++) {
                var k1 = keys[i];
                for (var j = i; j < keys.Length; j++) {
                    var k2 = keys[j];
                    var keysEqual = k1 == k2;
                    var keysMustBeEqual = i == j;
                    Out.WriteLine($"{k1} == {k2} -> {keysEqual} (expected: {keysMustBeEqual})");
                    if (keysMustBeEqual != keysEqual) {
                        var r = keysMustBeEqual.Equals(k1 == k2);
                        Assert.True(r);
                    }
                }
            }
        }
    }
}
