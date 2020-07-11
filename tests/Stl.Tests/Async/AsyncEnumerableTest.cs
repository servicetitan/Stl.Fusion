using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Testing;
using Stl.Time;
using Stl.Time.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Async
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class AsyncEnumerableTest : TestBase
    {
        public AsyncEnumerableTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public void BasicTest()
        {
            using var clock = new TestClock().SpeedupBy(10);

            Assert.Equal(
                new [] {"2", "4"}, 
                clock
                    .IntervalAsync(TimeSpan.FromMilliseconds(10))
                    .Skip(2)
                    .Take(3)
                    .Where(i => i != 3)
                    .Select(i => i.ToString())
                    .ToEnumerable());

            Assert.Equal(
                new [] {"2", "4"}, 
                clock
                    .IntervalAsync(TimeSpan.FromMilliseconds(10))
                    .Index()
#pragma warning disable 1998
                    .SkipWhile(async p => p.Index < 2)
                    .TakeWhile(async p => p.Index < 5 )
                    .Select(async p => p.Item)
                    .Where(async i => i != 3)
                    .Select(async i => i.ToString())
#pragma warning restore 1998
                    .ToEnumerable());
        }

        [Fact]
        public async Task ProperTerminationTest()
        {
            using var clock = new TestClock();
            var failed = false;

            async Task<int> Test() {
                await clock!.DelayAsync(10).ConfigureAwait(false);
                return (int) await clock!
                    .IntervalAsync(TimeSpan.FromMilliseconds(10))
                    .Select(i => {
                        if (i > 2) failed = true;
                        return i;
                    })
                    .Take(2)
                    .Count()
                    .ConfigureAwait(false);
            }

            var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(Test)).ToArray();
            var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            
            var whenAllTask = Task.WhenAll(tasks);
            await Task.WhenAny(whenAllTask, timeoutCts.Token.ToTask(false))
                .ConfigureAwait(false);
            
            Assert.False(timeoutCts.Token.IsCancellationRequested);
            
            var results = whenAllTask.Result;
            Assert.False(failed);
            Assert.True(results.All(r => r == 2));
        }
    }
}
