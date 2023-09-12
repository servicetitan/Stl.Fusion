using Stl.Testing.Collections;

namespace Stl.Tests.Channels;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class TransformTest
{
    [Fact]
    public async Task BasicTest()
    {
        await Test(3, 2);
        await Test(20, 7);
    }

    [Fact]
    public async Task ComplexTest()
    {
        var tests = new [] {
            Test(1, 1),
            Test(2, 1),
            Test(2, 2),
            Test(3, 2),
            Test(3, 1),
            Test(4, 2),
            Test(4, 3),
        };
        await Task.WhenAll(tests);
    }

    private async Task Test(int itemCount, int concurrencyLevel)
    {
        async Task TestOne(int? roundDuration, Func<Channel<int>, Channel<int>, Task> transform)
        {
            var source = Enumerable.Range(0, itemCount).ToArray();
            var cSource = source.ToUnboundedChannel();
            var cTarget = Channel.CreateUnbounded<int>();

            var start = Stopwatch.StartNew();
            await transform(cSource, cTarget).ConfigureAwait(false);

            var elapsed = start.ElapsedMilliseconds;
            var target = cTarget.Reader.ToAsyncEnumerable().ToEnumerable().ToArray();
            target.Should().BeEquivalentTo(source);
            var expectedRounds = itemCount / concurrencyLevel +
                (itemCount % concurrencyLevel != 0 ? 1 : 0);
            if (roundDuration.HasValue)
                (elapsed - roundDuration * expectedRounds).Should().BeInRange(-50, roundDuration.Value - 1);
        }

        await TestOne(100, (s, t) => s.Reader.ConcurrentTransform(t.Writer,
            async i => {
                await Task.Delay(100).ConfigureAwait(false);
                return i;
            },
            concurrencyLevel,
            ChannelCopyMode.CopyAllSilently));
        await TestOne(null, (s, t) => s.Reader.ConcurrentTransform(t.Writer,
            i => i,
            concurrencyLevel,
            ChannelCopyMode.CopyAllSilently));
        await TestOne(null,
            (s, t) => s.Reader.Transform(t.Writer,
            async i => {
                await Task.Delay(1).ConfigureAwait(false);
                return i;
            },
            ChannelCopyMode.CopyAllSilently));
        await TestOne(null, (s, t) => s.Reader.Transform(
            t.Writer,
            i => i,
            ChannelCopyMode.CopyAllSilently));
    }
}
