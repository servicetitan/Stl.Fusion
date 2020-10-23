using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using FluentAssertions;
using Stl.Async;
using Stl.Channels;
using Xunit;

namespace Stl.Tests.Channels
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class TransformTest
    {
        [Fact]
        public async Task BasicTest()
        {
            await TestAsync(3, 2);
            await TestAsync(20, 7);
        }

        [Fact]
        public async Task ComplexTest()
        {
            var tests = new [] {
                TestAsync(1, 1),
                TestAsync(2, 1),
                TestAsync(2, 2),
                TestAsync(3, 2),
                TestAsync(3, 1),
                TestAsync(4, 2),
                TestAsync(4, 3),
            };
            await Task.WhenAll(tests);
        }

        private async Task TestAsync(int itemCount, int concurrencyLevel)
        {
            async Task TestOne(int? roundDuration, Func<Channel<int>, Channel<int>, Task> transform)
            {
                var source = Enumerable.Range(0, itemCount).ToArray();
                var cSource = await source.ToUnboundedChannel();
                var cTarget = Channel.CreateUnbounded<int>();

                var start = Stopwatch.StartNew();
                await transform.Invoke(cSource, cTarget).ConfigureAwait(false);

                var elapsed = start.ElapsedMilliseconds;
                var target = cTarget.ToAsyncEnumerable().ToEnumerable().ToArray();
                target.Should().BeEquivalentTo(source);
                var expectedRounds = itemCount / concurrencyLevel +
                    (itemCount % concurrencyLevel != 0 ? 1 : 0);
                if (roundDuration.HasValue)
                    (elapsed - roundDuration * expectedRounds).Should().BeInRange(-50, roundDuration.Value - 1);

            }

            await TestOne(100, (s, t) => s.Reader.ConcurrentTransformAsync(t.Writer,
                async i => {
                    await Task.Delay(100).ConfigureAwait(false);
                    return i;
                }, concurrencyLevel));
            await TestOne(null, (s, t) => s.Reader.ConcurrentTransformAsync(t.Writer,
                i => i, concurrencyLevel));
            await TestOne(null, (s, t) => s.Reader.TransformAsync(t.Writer,
                async i => {
                    await Task.Delay(1).ConfigureAwait(false);
                    return i;
                }));
            await TestOne(null, (s, t) => s.Reader.TransformAsync(t.Writer,
                i => i));
        }
    }
}
