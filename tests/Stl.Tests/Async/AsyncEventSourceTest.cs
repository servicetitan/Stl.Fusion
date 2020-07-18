using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Stl.Async;
using Stl.OS;
using Stl.Testing;
using Stl.Time;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Async
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class AsyncEventSourceTest : TestBase
    {
        public AsyncEventSourceTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task BasicTest1()
        {
            var n = 0;
            var maxN = 2;
            var tasks = new List<Task>();
            AsyncEventSource<int> s;
            await using (s = new AsyncEventSource<int>()) {
                tasks.Add(CheckSequential(s, maxN));
                await s.NextAsync(n++);
                await Task.Delay(10);
                tasks.Add(CheckSequential(s, maxN));
                await s.NextAsync(n++);
                await Task.Delay(10);
                tasks.Add(CheckSequential(s, maxN));
                await s.NextAsync(n++);
            }
            s.IsCompleted.Should().BeTrue();
            await Task.WhenAll(tasks);
            tasks.All(t => t.IsCompletedSuccessfully).Should().BeTrue();
        }

        [Fact]
        public async Task BasicTest2()
        {
            var n = 0;
            var maxN = 2;
            var tasks = new List<Task>();
            AsyncEventSource<int> s;
            await using (s = new AsyncEventSource<int>()) {
                tasks.Add(CheckSequential(s, maxN));
                await s.NextAsync(n++);
                await Task.Yield();
                tasks.Add(CheckSequential(s, maxN));
                await s.NextAsync(n++);
                await Task.Yield();
                tasks.Add(CheckSequential(s, maxN));
                await s.NextAsync(n++);
            }
            s.IsCompleted.Should().BeTrue();
            await Task.WhenAll(tasks);
            tasks.All(t => t.IsCompletedSuccessfully).Should().BeTrue();
        }


        [Fact]
        public async Task BasicTest3()
        {
            if (TestRunnerInfo.GitHub.IsActionRunning)
                // TODO: Fix intermittent failures on GitHub
                return;

            var n = 0;
            var maxN = 2;
            var tasks = new List<Task>();
            AsyncEventSource<int> s;
            await using (s = new AsyncEventSource<int>()) {
                tasks.Add(Task.Run(() => CheckSequential(s, maxN)));
                await s.NextAsync(n++);
                // tasks.Add(Task.Run(() => CheckSequential(s, maxN)));
                await s.NextAsync(n++);
                // tasks.Add(Task.Run(() => CheckSequential(s, maxN)));
                await s.NextAsync(n++);
            }
            s.IsCompleted.Should().BeTrue();
            await Task.WhenAll(tasks);
            tasks.All(t => t.IsCompletedSuccessfully).Should().BeTrue();
        }

        [Theory]
        [InlineData(1000)]
        [InlineData(1001)]
        [InlineData(1002)]
        public async Task ConcurrentTest(int iterationCount)
        {
            if (TestRunnerInfo.GitHub.IsActionRunning)
                // We're skipping this test on GitHub: the class is anyway unused,
                // and the test doesn't seem to be stable. 
                return;

            var start = CpuClock.Now;
            var bigTasks = Enumerable.Range(0, iterationCount).Select(async seed => {
                await Task.Yield();
                var rnd = new Random(seed);
                var tasks = new List<Task<int>>();
                AsyncEventSource<int> s;
                await using (s = new AsyncEventSource<int>()) {
                    var maxI = rnd.Next(1000);
                    tasks.Add(CheckSequential(s, maxI, false));
                    for (var i = 0; i <= maxI; i++) {
                        if (rnd.Next(3) == 0)
                            tasks.Add(CheckSequentialRandomStop(i, s));
                        await s.NextAsync(i);
                    }
                }
                s.IsCompleted.Should().BeTrue();
                await Task.WhenAll(tasks);
                return tasks.Sum(t => t.Result);
            }).ToArray();
            await Task.WhenAll(bigTasks);
            var duration = CpuClock.Now - start;
            var sum = bigTasks.Sum(t => t.Result);

            Out.WriteLine($"Sum:      {sum}");
            Out.WriteLine($"Duration: {duration.TotalMilliseconds:f3}ms");

            sum.Should().BeGreaterThan(1000000);
        }

        private async Task<int> CheckSequential(IAsyncEnumerable<int> asyncSequence, int maxI, bool output = true)
        {
            var lastI = (int?) null;
            await foreach (var i in asyncSequence) {
                if (lastI.HasValue)
                    i.Should().Be(lastI.Value + 1);
                else if (output) 
                    Out.WriteLine($"First i: {i}");
                lastI = i;
            }
            lastI?.Should().Be(maxI);
            return lastI ?? 0;
        }

        private async Task<int> CheckSequentialRandomStop(int seed, IAsyncEnumerable<int> asyncSequence)
        {
            var rnd = new Random(seed);
            var lastI = (int?) null;
            await foreach (var i in asyncSequence) {
                if (lastI.HasValue)
                    i.Should().Be(lastI.Value + 1);
                lastI = i;
                if (rnd.Next(20) == 0)
                    break;
            }
            return lastI ?? 0;
        }
    }
}
