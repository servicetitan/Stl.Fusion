using Stl.Locking;
using Stl.OS;
using Stl.Testing.Collections;

namespace Stl.Tests.Async;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public abstract class AsyncLockTestBase(ITestOutputHelper @out) : TestBase(@out)
{
    protected class Resource(ITestOutputHelper? @out, IAsyncLock @lock)
    {
        private volatile int _lockCount;

        public readonly ITestOutputHelper? Out = @out;
        public readonly IAsyncLock Lock = @lock;

        public async Task Access(
            int workerId, int depth,
            int delayMs, int durationMs, int cancelMs, int maxDepth,
            int expectedLockCount = 0)
        {
            delayMs = Math.Max(0, delayMs);
            durationMs = Math.Max(0, durationMs);
            cancelMs = Math.Max(0, cancelMs);

            CancellationToken cancellationToken = default;
            var cts = (CancellationTokenSource)null!;
            if (cancelMs < durationMs) {
                cts = new CancellationTokenSource();
                cancellationToken = cts.Token;
                cts.CancelAfter(delayMs + cancelMs);
            }
            var message = "unlocked";
            try {
                await Delay(delayMs, cancellationToken).ConfigureAwait(false);

                Out?.WriteLine($"{workerId}.{depth}: locking");
                using var releaser = await Lock.Lock(cancellationToken).ConfigureAwait(false);
                releaser.MarkLockedLocally();
                Out?.WriteLine($"{workerId}.{depth}: locked");

                var lockCount = Interlocked.Increment(ref _lockCount);
                lockCount.Should().Be(expectedLockCount + 1);
                try {
                    var delayTask = Delay(durationMs, cancellationToken);
                    if (depth < maxDepth)
                        await Access(workerId, depth + 1, 0, durationMs / 2, cancelMs / 2, maxDepth, lockCount)
                            .ConfigureAwait(false);
                    await delayTask.ConfigureAwait(false);
                }
                finally {
                    Interlocked.Decrement(ref _lockCount).Should().Be(expectedLockCount);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                message = "cancelled";
            }
            finally {
                Out?.WriteLine($"{workerId}.{depth}: {message}");
                cts?.Dispose();
            }
        }

        private async ValueTask Delay(int delayMs, CancellationToken cancellationToken)
        {
            await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);

            // Let's add a "spread" that's not ms-based
            var sw = new SpinWait();
            var spinCount = (delayMs * 347) & 7;
            for (var i = 0; i < spinCount; i++)
#if !NETFRAMEWORK
                sw.SpinOnce(-1); // Unused in WASM
#else
                sw.SpinOnce(); // Unused in WASM
#endif
        }
    }

    protected abstract IAsyncLock CreateAsyncLock(LockReentryMode reentryMode);
    protected abstract void AssertResourcesReleased();

    [Fact]
    public async Task BasicTest()
    {
        const int NoCancel = 1_000;
        var r = new Resource(Out, CreateAsyncLock(LockReentryMode.CheckedFail));
        var workerId = 0;
        var tasks = new List<Task> {
            Task.Run(() => r.Access(workerId++, 0, 0, 3, NoCancel, 0)),
            Task.Run(() => r.Access(workerId++, 0, 0, 3, NoCancel, 0)),
            Task.Run(() => r.Access(workerId++, 0, 1, 3, NoCancel, 0)),
            Task.Run(() => r.Access(workerId++, 0, 1, 3, NoCancel, 0)),
            Task.Run(() => r.Access(workerId++, 0, 2, 3, NoCancel, 0)),
            Task.Run(() => r.Access(workerId++, 0, 2, 3, NoCancel, 0)),
        };
        await Task.WhenAll(tasks);
        tasks.All(t => t.IsCompletedSuccessfully()).Should().BeTrue();

        AssertResourcesReleased();
    }

    [Fact]
    public async Task ReentryTest()
    {
        const int NoCancel = 1_000;
        var r = new Resource(Out, CreateAsyncLock(LockReentryMode.CheckedFail));
        await Task.Run(() => r.Access(0, 0, 0, 3, NoCancel, 1))
            .AsAsyncFunc().Should().ThrowAsync<InvalidOperationException>();

        r = new Resource(Out, CreateAsyncLock(LockReentryMode.CheckedPass));
        await Task.Run(() => r.Access(1, 0, 0, 3, NoCancel, 1))
            .AsAsyncFunc().Should().CompleteWithinAsync(TimeSpan.FromSeconds(1));

        AssertResourcesReleased();
    }

    [Fact]
    public async Task ConcurrentTest()
    {
        var r = new Resource(null, CreateAsyncLock(LockReentryMode.CheckedPass));
        var rnd = new Random();
        var tasks = new List<Task>();

        var taskCount = TestRunnerInfo.IsBuildAgent() ? 2 : HardwareInfo.GetProcessorCountFactor(20);
        var maxDelayMs = 100;
        var maxDurationMs = 20;
        var maxDepth = 5;

        for (var i = 0; i < taskCount; i++) {
            var workerId = i;
            var delayMs = rnd.Next(maxDelayMs);
            var durationMs = rnd.Next(maxDurationMs);
            var cancelMs = rnd.Next(maxDurationMs * 2);
            var depth = rnd.Next(maxDepth);
            var task = Task.Run(() => r.Access(workerId, 0, delayMs, durationMs, cancelMs, depth));
            tasks.Add(task);
        }

        var expectedRuntime = TimeSpan.FromMilliseconds(
            1000 + taskCount * (maxDelayMs + 2 * (maxDurationMs + 20)));
        Out.WriteLine($"Expected runtime: {expectedRuntime.Seconds:f1}s");
        var start = CpuClock.Now;
        await Task.WhenAll(tasks).AsAsyncFunc()
            .Should().CompleteWithinAsync(expectedRuntime);
        var runtime = CpuClock.Now - start;
        Out.WriteLine($"Actual runtime:   {runtime.Seconds:f1}s");

        tasks.All(t => t.IsCompletedSuccessfully()).Should().BeTrue();

        AssertResourcesReleased();
    }
}
