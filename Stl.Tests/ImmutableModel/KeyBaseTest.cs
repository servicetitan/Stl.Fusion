using Stl.ImmutableModel;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.ImmutableModel 
{
    public class KeyBaseTest : TestBase
    {
        public KeyBaseTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public void KeyComparisonTest()
        {
            var keys = new KeyBase[] {
                KeyBase.Parse("k1"),
                KeyBase.Parse("k1|k2"),
                KeyBase.Parse("\\@whatever"),
                KeyBase.Parse("@property|k1"),
                KeyBase.Parse("k1|@property|k2"),
                KeyBase.Parse("#1"),
                KeyBase.Parse("#2"),
                KeyBase.Parse("\\#1"),
                KeyBase.Parse("\\#1|k1"),
                KeyBase.Parse("#1|k1"),
                KeyBase.Parse("k1|\\#1"),
                KeyBase.Parse("k1|#1"),
                KeyBase.DefaultRootKey,
                KeyBase.Undefined,
            };

            for (var i = 0; i < keys.Length; i++) {
                var k1 = keys[i];
                for (var j = 0; j < keys.Length; j++) {
                    var k2 = keys[j];
                    k2 = KeyBase.Parse(k2.Format()); // Intentional
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
