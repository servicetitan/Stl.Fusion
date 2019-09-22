using System;
using System.Linq;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Async
{
    public class AsyncEnumerableTest : TestBase
    {
        public AsyncEnumerableTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public void BasicTest()
        {
                Assert.Equal(
                    new [] {"2", "4"}, 
                    AsyncEnumerable
                        .Interval(TimeSpan.Zero)
                        .Skip(2)
                        .Take(3)
                        .Where(i => i != 3)
                        .Select(i => i.ToString())
                        .ToEnumerable());

                Assert.Equal(
                    new [] {"2", "4"}, 
                    AsyncEnumerable
                        .Interval(TimeSpan.Zero)
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
            var failed = false;

            async Task<int> Test() {
                await Task.Delay(100);
                return AsyncEnumerable
                    .Interval(TimeSpan.Zero)
                    .Select(i => {
                        if (i > 2) failed = true;
                        return i;
                    })
                    .Take(2)
                    .ToEnumerable()
                    .Count();
            }

            var tasks = Enumerable.Range(0, 100000).Select(i => Task.Run(Test)).ToArray();
            var results = await Task.WhenAll(tasks);
            Assert.False(failed);
            Assert.True(results.All(r => r == 2));
        }
    }
}
