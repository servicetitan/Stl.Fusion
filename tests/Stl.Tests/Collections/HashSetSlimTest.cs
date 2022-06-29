using Stl.Collections.Slim;

namespace Stl.Tests.Collections;

public class HashSetSlimTest
{
    private string[] Items { get; } =
        Enumerable.Range(0, 26).Select(i => ((char) ('a' + i % 26)).ToString()).ToArray();

    [Fact]
    public void HashSetSlim_CombinedTest()
    {
        Items.Distinct().Count().Should().Be(26);
        Items.Length.Should().Be(26);

        Test(new HashSetSlim1<string>());
        Test(new HashSetSlim2<string>());
        Test(new HashSetSlim3<string>());
        Test(new HashSetSlim4<string>());
    }

    [Fact]
    public void SafeHashSetSlim_CombinedTest()
    {
        Test(new SafeHashSetSlim1<string>());
        Test(new SafeHashSetSlim2<string>());
        Test(new SafeHashSetSlim3<string>());
        Test(new SafeHashSetSlim4<string>());
    }

    private void Test(IHashSetSlim<string> c)
    {
        var hs = new HashSet<string>(StringComparer.Ordinal);
        var rnd = new Random();
        for (var pass = 0; pass < 1000; pass++) {
            var opCount = pass * 2;
            for (var i = 0; i < opCount; i++) {
                var item = Items[rnd.Next(Items.Length)];
                c.Contains(item).Should().Be(hs.Contains(item));
                c.Count.Should().Be(hs.Count);
                var rndValue = rnd.NextDouble();
                if (rndValue < 0.55) {
                    c.Add(item);
                    hs.Add(item);
                }
                else if (rndValue < 0.9) {
                    c.Remove(item);
                    hs.Remove(item);
                }
                else {
                    c.Clear();
                    hs.Clear();
                }
                c.Contains(item).Should().Be(hs.Contains(item));
            }
            c.Items.Should().BeEquivalentTo(hs);
            var sum = hs.Sum(item => item.Sum(c => (long) c));

            // Apply
            var applyHandler = (Action<Box<long>, string>) (
                (box, item) => box.Value += item.Sum(c => (long) c));
            var box1 = new Box<long>();
            c.Apply(box1, applyHandler);
            box1.Value.Should().Be(sum);

            // Aggregate w/ ref
            long sum2 = 0;
            var aggregateHandler1 = (Aggregator<long, string>) (
                delegate(ref long s, string item) {
                    s += item.Sum(c => (long) c);
                });
            c.Aggregate(ref sum2, aggregateHandler1);
            sum2.Should().Be(sum);

            // Aggregate
            var aggregateHandler2 = (Func<long, string, long>) (
                (s, item) => s + item.Sum(c => (long) c));
            var sum3 = c.Aggregate(0L, aggregateHandler2);
            sum3.Should().Be(sum);

            // CopyTo
            var array = new string[hs.Count];
            c.CopyTo(array.AsSpan());
            array.Should().BeEquivalentTo(hs);
        }
    }
}
