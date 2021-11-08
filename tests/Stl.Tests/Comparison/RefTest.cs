using Stl.Comparison;

namespace Stl.Tests.Comparison;

public class RefTest
{
    public record TestRecord(string X) { }

    [Fact]
    public void BasicTest()
    {
        var r0 = Ref.New(default(TestRecord));
        var r1 = Ref.New(new TestRecord("X"));
        var r2 = Ref.New(new TestRecord("X"));
        r0.Target.Should().NotBe(r1.Target);
        r1.Target.Should().Be(r2.Target);

        var refs = new[] {r0!, r1, r2};
        for (var i1 = 0; i1 < refs.Length; i1++) {
            r1.GetHashCode();
            for (var i2 = 0; i2 < refs.Length; i2++) {
                if (i1 == i2)
                    refs[i1].Should().Be(refs[i2]);
                else
                    refs[i1].Should().NotBe(refs[i2]);
            }
        }
    }
}
