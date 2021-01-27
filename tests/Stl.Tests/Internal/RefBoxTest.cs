using FluentAssertions;
using Stl.Internal;
using Xunit;

namespace Stl.Tests.Internal
{
    public class RefBoxTest
    {
        public record TestRecord(string X) { }

        [Fact]
        public void BasicTest()
        {
            var b0 = RefBox.New(default(TestRecord));
            var b1 = RefBox.New(new TestRecord("X"));
            var b2 = RefBox.New(new TestRecord("X"));
            b0.Target.Should().NotBe(b1.Target);
            b1.Target.Should().Be(b2.Target);

            var boxes = new[] {b0!, b1, b2};
            for (var i1 = 0; i1 < boxes.Length; i1++) {
                b1.GetHashCode();
                for (var i2 = 0; i2 < boxes.Length; i2++) {
                    if (i1 == i2)
                        boxes[i1].Should().Be(boxes[i2]);
                    else
                        boxes[i1].Should().NotBe(boxes[i2]);
                }
            }
        }
    }
}
