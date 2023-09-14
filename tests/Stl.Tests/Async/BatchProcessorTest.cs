using Stl.Testing.Collections;

namespace Stl.Tests.Async;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class BatchProcessorTest : TestBase
{
    public BatchProcessorTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public async Task BasicTest()
    {
        var batchIndex = 0;
        await using var processor = new BatchProcessor<int, (int BatchIndex, int Value)>() {
            MinWorkerCount = 1,
            MaxWorkerCount = 3,
            BatchSize = 3,
            Implementation = async (batch, cancellationToken) => {
                var bi = Interlocked.Increment(ref batchIndex);
                await Task.Delay(100).ConfigureAwait(false);
                foreach (var item in batch) {
                    if (item.Input == -1000)
                        throw new InvalidOperationException();
                    item.SetResult((bi, item.Input), cancellationToken);
                }
            }
        };

        async Task Reset() {
            await processor.Reset();
            Interlocked.Exchange(ref batchIndex, 0);
        }

        // Batch formation tests
        await Reset();
        var r = await processor.Process(1);
        r.Should().Be((1, 1));

        await Reset();
        var tasks = Enumerable.Range(0, 4).Select(i => processor.Process(i)).ToArray();
        await Task.WhenAll(tasks);
        tasks.Count(t => t.Result.BatchIndex == 1).Should().Be(1);
        tasks.Count(t => t.Result.BatchIndex == 2).Should().Be(3);
        tasks.Select(t => t.Result.Value).Should().BeEquivalentTo(Enumerable.Range(0, 4));

        await Reset();
        tasks = Enumerable.Range(0, 6).Select(i => processor.Process(i)).ToArray();
        await Task.WhenAll(tasks);
        tasks.Count(t => t.Result.BatchIndex == 1).Should().Be(1);
        tasks.Count(t => t.Result.BatchIndex == 2).Should().Be(3);
        tasks.Count(t => t.Result.BatchIndex == 3).Should().Be(2);
        tasks.Select(t => t.Result.Value).Should().BeEquivalentTo(Enumerable.Range(0, 6));

        await Reset();
        tasks = Enumerable.Range(0, 10).Select(i => processor.Process(i)).ToArray();
        await Task.WhenAll(tasks);
        tasks.Count(t => t.Result.BatchIndex == 1).Should().Be(1);
        tasks.Count(t => t.Result.BatchIndex == 2).Should().Be(3);
        tasks.Count(t => t.Result.BatchIndex == 3).Should().Be(3);
        tasks.Count(t => t.Result.BatchIndex == 4).Should().Be(3);
        tasks.Select(t => t.Result.Value).Should().BeEquivalentTo(Enumerable.Range(0, 10));

        // Cancellation test
        await Reset();
        using var cts1 = new CancellationTokenSource();
        using var cts2 = new CancellationTokenSource();
        tasks = Enumerable.Range(0, 4)
            .Select(i => processor.Process(i, i % 2 == 0 ? cts1.Token : cts2.Token))
            .Select(t => t.SuppressCancellation())
            .ToArray();
        await Delay(0.02);
        cts1.Cancel();
        await Task.WhenAll(tasks);
        tasks.Count(t => t.Result == (0, 0)).Should().Be(1);
        tasks.Count(t => t.Result.BatchIndex == 1).Should().Be(1);
        tasks.Count(t => t.Result.BatchIndex == 2).Should().Be(2);

        // Error test
        await Reset();
        tasks = new [] {0, 1, 2, -1000}
            .Select(i => processor.Process(i, default))
            .ToArray();
        try {
            await Task.WhenAll(tasks);
            true.Should().BeFalse("No exception was thrown.");
        }
        catch (InvalidOperationException) {
            // Intended
        }
    }

    [Fact(Timeout = 30_000)]
    public async Task WorkerRampUpTest()
    {
        if (TestRunnerInfo.IsBuildAgent())
            return;

        await using var processor = new BatchProcessor<int, int>() {
            MinWorkerCount = 1,
            MaxWorkerCount = 100,
            BatchSize = 10,
            WorkerCollectionPeriod = TimeSpan.FromSeconds(1),
            Implementation = async (batch, cancellationToken) => {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                foreach (var item in batch)
                    item.SetResult(item.Input);
            }
        };

        var tasks = new Task<int>[10_000];
        for (var i = 0; i < tasks.Length; i++)
            tasks[i] = processor.Process(i);
        while (true) {
            await Delay(0.5);
            Out.WriteLine($"WorkerCount: {processor.GetWorkerCount()}");
            if (processor.GetWorkerCount() > 60)
                break;
        }
        while (true) {
            await Delay(0.5);
            Out.WriteLine($"WorkerCount: {processor.GetWorkerCount()}");
            if (processor.GetWorkerCount() == 1)
                break;
        }
        await Task.WhenAll(tasks);
        tasks.Where((t, i) => t.Result != i).Count().Should().Be(0);
    }
}
