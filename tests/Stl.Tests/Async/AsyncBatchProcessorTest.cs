using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Stl.Async;
using Xunit;

namespace Stl.Tests.Async
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class AsyncBatchProcessorTest
    {
        [Fact]
        public async Task BasicTest()
        {
            var batchIndex = 0;
            await using var processor = new AsyncBatchProcessor<int, (int, int)>() {
                ConcurrencyLevel = 2,
                MaxBatchSize = 3,
                BatchProcessor = async (batch, cancellationToken) => {
                    var bi = Interlocked.Increment(ref batchIndex);
                    await Task.Delay(100).ConfigureAwait(false);
                    foreach (var item in batch) {
                        if (!item.TryCancel(cancellationToken))
                            item.SetResult((bi, item.Input), cancellationToken);
                    }
                }
            };
            processor.RunAsync().Ignore();

            async Task BeginAsync()
            {
                Interlocked.Exchange(ref batchIndex, -2);
                var t1 = processor.ProcessAsync(-1);
                await Task.Delay(25);
                var t2 = processor.ProcessAsync(-2);
                await Task.Delay(25);
            }

            // Batch formation tests
            await BeginAsync();
            var r = await processor.ProcessAsync(1);
            r.Should().Be((1, 1));

            await BeginAsync();
            var tasks = Enumerable.Range(0, 4).Select(i => processor.ProcessAsync(i)).ToArray();
            await Task.WhenAll(tasks);
            tasks.Count(t => t.Result.Item1 == 1).Should().Be(3);
            tasks.Count(t => t.Result.Item1 == 2).Should().Be(1);
            tasks.Select(t => t.Result.Item2).Should().BeEquivalentTo(Enumerable.Range(0, 4));

            await BeginAsync();
            tasks = Enumerable.Range(0, 6).Select(i => processor.ProcessAsync(i)).ToArray();
            await Task.WhenAll(tasks);
            tasks.Count(t => t.Result.Item1 == 1).Should().Be(3);
            tasks.Count(t => t.Result.Item1 == 2).Should().Be(3);
            tasks.Select(t => t.Result.Item2).Should().BeEquivalentTo(Enumerable.Range(0, 6));

            await BeginAsync();
            tasks = Enumerable.Range(0, 10).Select(i => processor.ProcessAsync(i)).ToArray();
            await Task.WhenAll(tasks);
            tasks.Count(t => t.Result.Item1 == 1).Should().Be(3);
            tasks.Count(t => t.Result.Item1 == 2).Should().Be(3);
            tasks.Count(t => t.Result.Item1 == 3).Should().Be(3);
            tasks.Count(t => t.Result.Item1 == 4).Should().Be(1);
            tasks.Select(t => t.Result.Item2).Should().BeEquivalentTo(Enumerable.Range(0, 10));

            // Cancellation test
            await BeginAsync();
            using var cts1 = new CancellationTokenSource();
            using var cts2 = new CancellationTokenSource();
            tasks = Enumerable.Range(0, 4)
                .Select(i => processor.ProcessAsync(i, i % 2 == 0 ? cts1.Token : cts2.Token))
                .Select(t => t.SuppressCancellation())
                .ToArray();
            cts1.Cancel();
            await Task.WhenAll(tasks);
            tasks.Count(t => t.Result == (0, 0)).Should().Be(2);
            tasks.Count(t => t.Result.Item1 == 1).Should().Be(1);
            tasks.Count(t => t.Result.Item1 == 2).Should().Be(1);
        }
    }
}
