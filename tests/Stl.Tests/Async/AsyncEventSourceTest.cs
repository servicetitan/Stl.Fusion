using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Stl.Async;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Async
{
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
                await s.PublishAsync(n++);
                await Task.Delay(10);
                tasks.Add(CheckSequential(s, maxN));
                await s.PublishAsync(n++);
                await Task.Delay(10);
                tasks.Add(CheckSequential(s, maxN));
                await s.PublishAsync(n++);
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
                await s.PublishAsync(n++);
                await Task.Yield();
                tasks.Add(CheckSequential(s, maxN));
                await s.PublishAsync(n++);
                await Task.Yield();
                tasks.Add(CheckSequential(s, maxN));
                await s.PublishAsync(n++);
            }
            s.IsCompleted.Should().BeTrue();
            await Task.WhenAll(tasks);
            tasks.All(t => t.IsCompletedSuccessfully).Should().BeTrue();
        }


        [Fact]
        public async Task BasicTest3()
        {
            var n = 0;
            var maxN = 2;
            var tasks = new List<Task>();
            AsyncEventSource<int> s;
            await using (s = new AsyncEventSource<int>()) {
                tasks.Add(Task.Run(() => CheckSequential(s, maxN)));
                await s.PublishAsync(n++);
                // tasks.Add(Task.Run(() => CheckSequential(s, maxN)));
                await s.PublishAsync(n++);
                // tasks.Add(Task.Run(() => CheckSequential(s, maxN)));
                await s.PublishAsync(n++);
            }
            s.IsCompleted.Should().BeTrue();
            await Task.WhenAll(tasks);
            tasks.All(t => t.IsCompletedSuccessfully).Should().BeTrue();
        }

        [Fact]
        public async Task ConcurrentTest()
        {
            var bigTasks = Enumerable.Range(0, 2000).Select(async seed => {
                await Task.Yield();
                var rnd = new Random(seed);
                var tasks = new List<Task>();
                AsyncEventSource<int> s;
                await using (s = new AsyncEventSource<int>()) {
                    var maxI = rnd.Next(2000);
                    tasks.Add(CheckSequential(s, maxI));
                    for (var i = 0; i <= maxI; i++) {
                        if (rnd.Next(3) == 0)
                            tasks.Add(CheckSequentialRandomStop(i, s));
                        await s.PublishAsync(i);
                    }
                }
                s.IsCompleted.Should().BeTrue();
                await Task.WhenAll(tasks);
                tasks.All(t => t.IsCompletedSuccessfully).Should().BeTrue();
            }).ToArray();
            await Task.WhenAll(bigTasks);
            bigTasks.All(t => t.IsCompletedSuccessfully).Should().BeTrue();
        }

        private async Task CheckSequential(IAsyncEnumerable<int> asyncSequence, int maxI)
        {
            var lastI = (int?) null;
            await foreach (var i in asyncSequence) {
                if (lastI.HasValue)
                    i.Should().Be(lastI.Value + 1);
                else 
                    Out.WriteLine($"First i: {i}");
                lastI = i;
            }
            lastI?.Should().Be(maxI);
        }

        private async Task CheckSequentialRandomStop(int seed, IAsyncEnumerable<int> asyncSequence)
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
        }
    }
}
