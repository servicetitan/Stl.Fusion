using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
    [Category(nameof(TimeSensitiveTests))]
    public class AsyncLockTest : TestBase
    {
        public class Resource
        {
            private volatile int _lockCount = 0;

            public readonly AsyncLock Lock;

            public Resource(AsyncLock @lock) => Lock = @lock;

            public async Task AccessAsync(int delayMs, int durationMs, int cancelMs, int reentryCount = 0, int expectedLockCount = 0)
            {
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
                var sw = new SpinWait();
                if (delayMs <= 0) {   
                    for (var i = 0; i < -delayMs; i++)
                        sw.SpinOnce(int.MaxValue);
                    return;
                }

                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                
                // Let's add a "spread" that's not ms-based
                for (var i = 0; i < delayMs; i++)
                    sw.SpinOnce(int.MaxValue);
            }
        }

        public AsyncLockTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task BasicTest()
        {
            var r = new Resource(new AsyncLock(ReentryMode.CheckedFail));
            var tasks = new List<Task> {
                Task.Run(() => r.AccessAsync(0, 3, 10)),
                Task.Run(() => r.AccessAsync(0, 3, 10)),
                Task.Run(() => r.AccessAsync(1, 3, 10)),
                Task.Run(() => r.AccessAsync(1, 3, 10)),
                Task.Run(() => r.AccessAsync(2, 3, 10)),
                Task.Run(() => r.AccessAsync(2, 3, 10)),
            };
            await Task.WhenAll(tasks);
            tasks.All(t => t.IsCompletedSuccessfully).Should().BeTrue();
        }

        [Fact]
        public async Task ConcurrentTest()
        {
            var r = new Resource(new AsyncLock(ReentryMode.CheckedPass));
            var rnd = new Random();
            var tasks = new List<Task>();

            const int taskCount = 1000;
            const int maxDelayMs = 100;
            const int maxDurationMs = 3;
            for (var i = 0; i < taskCount; i++) {
                var delayMs = rnd.Next(maxDelayMs);
                var durationMs = rnd.Next(maxDurationMs);
                var cancelMs = int.MaxValue; // rnd.Next(maxDurationMs * 3);
                var task = Task.Run(() => r.AccessAsync(delayMs, durationMs, cancelMs, 5));
                tasks.Add(task);
            }

            Func<Task> whenAll = () => Task.WhenAll(tasks);
            var expectedRuntime = TimeSpan.FromMilliseconds(maxDelayMs + maxDurationMs * taskCount);
            
            Out.WriteLine($"Expected runtime: {expectedRuntime.Seconds:f1}s");
            var start = RealTimeClock.HighResolutionNow;
            await whenAll.Should().CompleteWithinAsync(10 * expectedRuntime);
            var runtime = RealTimeClock.HighResolutionNow - start;
            Out.WriteLine($"Actual runtime:   {runtime.Seconds:f1}s");

            tasks.All(t => t.IsCompletedSuccessfully).Should().BeTrue();
        }
    }
}
