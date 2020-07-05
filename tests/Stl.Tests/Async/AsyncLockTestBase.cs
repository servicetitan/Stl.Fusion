using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Stl.Locking;
using Stl.Testing;
using Stl.Time;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Async
{
    [Trait("Category", nameof(TimeSensitive))]
    public abstract class AsyncLockTestBase : TestBase
    {
        protected class Resource
        {
            private volatile int _lockCount = 0;

            public readonly IAsyncLock Lock;

            public Resource(IAsyncLock @lock) => Lock = @lock;

            public async Task AccessAsync(int delayMs, int durationMs, int cancelMs, int reentryCount = 0, int expectedLockCount = 0)
            {
                delayMs = Math.Max(0, delayMs);
                durationMs = Math.Max(0, durationMs);
                cancelMs = Math.Max(0, cancelMs);

                var cancellationToken = CancellationToken.None;
                var cts = (CancellationTokenSource) null!;
                if (cancelMs <= durationMs) {
                    cts = new CancellationTokenSource();
                    cancellationToken = cts.Token;
                    cts.CancelAfter(delayMs + cancelMs);
                }
                try {
                    await Delay(delayMs, cancellationToken).ConfigureAwait(false);

                    using (await Lock.LockAsync(cancellationToken).ConfigureAwait(false)) {
                        var lockCount = Interlocked.Increment(ref _lockCount);
                        lockCount.Should().Be(expectedLockCount + 1);
                        try {
                            var delayTask = Delay(durationMs, cancellationToken);
                            if (reentryCount > 0)
                                await AccessAsync(0, durationMs - 1, cancelMs - 2, reentryCount - 1, lockCount)
                                    .ConfigureAwait(false);
                            await delayTask.ConfigureAwait(false);
                        }
                        finally {
                            Interlocked.Decrement(ref _lockCount).Should().Be(expectedLockCount);
                        }
                    }
                }
                catch (OperationCanceledException) {
                    // It's fine here
                }
                finally {
                    cts?.Dispose();
                }
            }

            private async ValueTask Delay(int delayMs, CancellationToken cancellationToken)
            {
                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                
                // Let's add a "spread" that's not ms-based
                var sw = new SpinWait();
                var spinCount = (delayMs * 347) & 15;
                for (var i = 0; i < spinCount; i++)
                    sw.SpinOnce(int.MaxValue);
            }
        }

        public AsyncLockTestBase(ITestOutputHelper @out) : base(@out) { }

        protected abstract IAsyncLock CreateAsyncLock(ReentryMode reentryMode); 
        protected abstract void AssertResourcesReleased();

        [Fact]
        public async Task BasicTest()
        {
            const int NoCancel = 1_000;
            var r = new Resource(CreateAsyncLock(ReentryMode.CheckedFail));
            var tasks = new List<Task> {
                Task.Run(() => r.AccessAsync(0, 3, NoCancel)),
                Task.Run(() => r.AccessAsync(0, 3, NoCancel)),
                Task.Run(() => r.AccessAsync(1, 3, NoCancel)),
                Task.Run(() => r.AccessAsync(1, 3, NoCancel)),
                Task.Run(() => r.AccessAsync(2, 3, NoCancel)),
                Task.Run(() => r.AccessAsync(2, 3, NoCancel)),
            };
            await Task.WhenAll(tasks);
            tasks.All(t => t.IsCompletedSuccessfully).Should().BeTrue();
            
            AssertResourcesReleased();
        }

        [Fact]
        public async Task ReentryTest()
        {
            const int NoCancel = 1_000;
            var r = new Resource(CreateAsyncLock(ReentryMode.CheckedFail));
            await Task.Run(() => r.AccessAsync(0, 3, NoCancel, 1))
                .AsAsyncFunc().Should().ThrowAsync<InvalidOperationException>();

            r = new Resource(CreateAsyncLock(ReentryMode.CheckedPass));
            await Task.Run(() => r.AccessAsync(0, 3, NoCancel, 1))
                .AsAsyncFunc().Should().CompleteWithinAsync(TimeSpan.FromSeconds(1));

            AssertResourcesReleased();
        }

        [Fact]
        public async Task ConcurrentTest()
        {
            var r = new Resource(CreateAsyncLock(ReentryMode.CheckedPass));
            var rnd = new Random();
            var tasks = new List<Task>();

            const int taskCount = 200;
            const int maxDelayMs = 100;
            const int maxDurationMs = 3;
            const int maxReentryCount = 5;
            for (var i = 0; i < taskCount; i++) {
                var delayMs = rnd.Next(maxDelayMs);
                var durationMs = rnd.Next(maxDurationMs);
                var cancelMs = rnd.Next(maxDurationMs * 3);
                var reentyCount = rnd.Next(maxReentryCount);
                var task = Task.Run(() => r.AccessAsync(delayMs, durationMs, cancelMs, reentyCount));
                tasks.Add(task);
            }

            var expectedRuntime = TimeSpan.FromMilliseconds(
                5 * maxDelayMs + maxDurationMs * maxReentryCount * taskCount);
            Out.WriteLine($"Expected runtime: {expectedRuntime.Seconds:f1}s");
            var start = CpuClock.Now;
            await Task.WhenAll(tasks).AsAsyncFunc()
                .Should().CompleteWithinAsync(expectedRuntime);
            var runtime = CpuClock.Now - start;
            Out.WriteLine($"Actual runtime:   {runtime.Seconds:f1}s");

            tasks.All(t => t.IsCompletedSuccessfully).Should().BeTrue();

            AssertResourcesReleased();
        }
    }
}
