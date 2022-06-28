using Stl.Testing.Collections;

namespace Stl.Tests.Async;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class BatchProcessorTest
{
    [Fact]
    public async Task BasicTest()
    {
        var batchIndex = 0;
        await using var processor = new BatchProcessor<int, (int, int)>() {
            ConcurrencyLevel = 2,
            MaxBatchSize = 3,
            Implementation = async (batch, cancellationToken) => {
                var bi = Interlocked.Increment(ref batchIndex);
                await Task.Delay(100).ConfigureAwait(false);
                foreach (var item in batch) {
                    if (item.Input == -1000)
                        throw new InvalidOperationException();
                    if (!item.TryCancel(cancellationToken))
                        item.SetResult((bi, item.Input), cancellationToken);
                }
            }
        };

        async Task Begin()
        {
            Interlocked.Exchange(ref batchIndex, -2);
            var t1 = processor.Process(-1);
            await Task.Delay(25);
            var t2 = processor.Process(-2);
            await Task.Delay(25);
        }

        // Batch formation tests
        await Begin();
        var r = await processor.Process(1);
        r.Should().Be((1, 1));

        await Begin();
        var tasks = Enumerable.Range(0, 4).Select(i => processor.Process(i)).ToArray();
        await Task.WhenAll(tasks);
        tasks.Count(t => t.Result.Item1 == 1).Should().Be(3);
        tasks.Count(t => t.Result.Item1 == 2).Should().Be(1);
        tasks.Select(t => t.Result.Item2).Should().BeEquivalentTo(Enumerable.Range(0, 4));

        await Begin();
        tasks = Enumerable.Range(0, 6).Select(i => processor.Process(i)).ToArray();
        await Task.WhenAll(tasks);
        tasks.Count(t => t.Result.Item1 == 1).Should().Be(3);
        tasks.Count(t => t.Result.Item1 == 2).Should().Be(3);
        tasks.Select(t => t.Result.Item2).Should().BeEquivalentTo(Enumerable.Range(0, 6));

        await Begin();
        tasks = Enumerable.Range(0, 10).Select(i => processor.Process(i)).ToArray();
        await Task.WhenAll(tasks);
        tasks.Count(t => t.Result.Item1 == 1).Should().Be(3);
        tasks.Count(t => t.Result.Item1 == 2).Should().Be(3);
        tasks.Count(t => t.Result.Item1 == 3).Should().Be(3);
        tasks.Count(t => t.Result.Item1 == 4).Should().Be(1);
        tasks.Select(t => t.Result.Item2).Should().BeEquivalentTo(Enumerable.Range(0, 10));

        // Cancellation test
        await Begin();
        using var cts1 = new CancellationTokenSource();
        using var cts2 = new CancellationTokenSource();
        tasks = Enumerable.Range(0, 4)
            .Select(i => processor.Process(i, i % 2 == 0 ? cts1.Token : cts2.Token))
            .Select(t => t.SuppressCancellation())
            .ToArray();
        cts1.Cancel();
        await Task.WhenAll(tasks);
        tasks.Count(t => t.Result == (0, 0)).Should().Be(2);
        tasks.Count(t => t.Result.Item1 == 1).Should().Be(1);
        tasks.Count(t => t.Result.Item1 == 2).Should().Be(1);

        // Error test
        await Begin();
        tasks = new [] {0, 1, 2, -1000}
            .Select(i => processor.Process(i, default))
            .ToArray();
        try {
            await Task.WhenAll(tasks);
            true.Should().BeFalse("No exception was thrown.");
        }
        catch (InvalidOperationException) { }
    }
}
