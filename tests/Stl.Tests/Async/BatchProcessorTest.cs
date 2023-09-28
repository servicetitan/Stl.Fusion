using Stl.Testing.Collections;

namespace Stl.Tests.Async;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class BatchProcessorTest(ITestOutputHelper @out) : TestBase(@out)
{
    [Fact]
    public async Task BasicTest()
    {
        var batchIndex = 0;
        await using var processor = new BatchProcessor<int, (int BatchIndex, int Value)>() {
            BatchSize = 3,
            WorkerPolicy = new BatchProcessorWorkerPolicy {
                MinWorkerCount = 1,
                MaxWorkerCount = 3,
            },
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
        tasks.Count(t => t.Result.BatchIndex > 1).Should().Be(5);
        tasks.Select(t => t.Result.Value).Should().BeEquivalentTo(Enumerable.Range(0, 6));

        await Reset();
        tasks = Enumerable.Range(0, 10).Select(i => processor.Process(i)).ToArray();
        await Task.WhenAll(tasks);
        tasks.Count(t => t.Result.BatchIndex == 1).Should().Be(1);
        tasks.GroupBy(t => t.Result.BatchIndex).All(g => g.Count() <= 3).Should().BeTrue();
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

        var batchDelay = 100;

        var services = CreateLoggingServices(Out);
        await using var processor = new BatchProcessor<int, int>() {
            BatchSize = 10,
            WorkerPolicy = new BatchProcessorWorkerPolicy() {
                MinWorkerCount = 1,
                MaxWorkerCount = 100,
                CollectorCycle = TimeSpan.FromSeconds(1),
            },
            Implementation = async (batch, cancellationToken) => {
                await Task.Delay(batchDelay, cancellationToken).ConfigureAwait(false);
                foreach (var item in batch)
                    item.SetResult(item.Input);
            },
            Log = services.LogFor<BatchProcessor<int, int>>(),
        };

        processor.GetWorkerCount().Should().Be(0);
        processor.GetPlannedWorkerCount().Should().Be(0);
        await processor.Process(0);
        processor.GetWorkerCount().Should().Be(1);
        processor.GetPlannedWorkerCount().Should().Be(1);

        batchDelay = 10;
        await Test(1000, 5);

        batchDelay = 100;
        await Test(1000, 10);
        await Test(10_000, 60);

        async Task Test(int taskCount, int minExpectedWorkerCount) {
            Out.WriteLine($"Task count: {taskCount}, batch delay: {batchDelay}");
            var tasks = new Task<int>[taskCount];
            for (var i = 0; i < tasks.Length; i++)
                tasks[i] = processor.Process(i);

            while (true) {
                await Delay(0.5);
                Out.WriteLine($"WorkerCount: {processor.GetWorkerCount()}");
                if (processor.GetWorkerCount() >= minExpectedWorkerCount)
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
            Out.WriteLine("");
        }
    }
}
