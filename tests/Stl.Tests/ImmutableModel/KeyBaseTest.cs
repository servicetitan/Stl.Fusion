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
            var keys = new Key?[] {
                Key.Parse("k1"),
                Key.Parse("k1|k2"),
                Key.Parse("\\@whatever"),
                Key.Parse("@property|k1"),
                Key.Parse("k1|@property|k2"),
                Key.Parse("@option|k1"),
                Key.Parse("k1|@option|k2"),
                Key.Parse("#1"),
                Key.Parse("#2"),
                Key.Parse("\\#1"),
                Key.Parse("\\#1|k1"),
                Key.Parse("#1|k1"),
                Key.Parse("k1|\\#1"),
                Key.Parse("k1|#1"),
                Key.DefaultRootKey,
                null,
            };

            for (var i = 0; i < keys.Length; i++) {
                var k1 = keys[i];
                for (var j = 0; j < keys.Length; j++) {
                    var k2 = keys[j];
                    if (k2 != null) {
                        k2 = Key.Parse(k2.Format());
                        k2 = k2.PassThroughJsonConvert();
                    }
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
