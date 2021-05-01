using System;
using System.Threading.Tasks;
using FluentAssertions;
using Stl.Async;
using Stl.Testing;
using Stl.Time;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Async
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class AsyncDisposableTest : TestBase
    {
        public class AsyncDisposableWithDelay : AsyncDisposableBase
        {
            public TimeSpan DisposeDelay { get; }

            public AsyncDisposableWithDelay(TimeSpan disposeDelay) => DisposeDelay = disposeDelay;

            protected override async ValueTask DisposeInternal(bool disposing)
            {
                await Task.Delay(DisposeDelay);
            }
        }

        public AsyncDisposableTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task DisposeAsyncTest()
        {
            if (TestRunnerInfo.IsBuildAgent())
                // TODO: Fix intermittent failures on GitHub
                return;
            AsyncDisposableWithDelay d;
            Task task;
            Moment start;
            await using (d = new AsyncDisposableWithDelay(TimeSpan.FromMilliseconds(500))) {
                Assert.Equal(DisposalState.Active, d.DisposalState);
                Out.WriteLine("Active.");
                Out.WriteLine("Dispose started.");
                task = Task.Run(async () => {
                    await Task.Delay(100);
                    Assert.Equal(DisposalState.Disposing, d!.DisposalState);
                    Out.WriteLine("Disposing check passed.");
                });
                start = CpuClock.Now;
            }
            Assert.Equal(DisposalState.Disposed, d.DisposalState);
            (CpuClock.Now - start).Should().BeCloseTo(d.DisposeDelay, TimeSpan.FromMilliseconds(200));
            await task;
            Out.WriteLine("Dispose completed.");
        }

        [Fact]
        public void DisposeTest()
        {
            AsyncDisposableWithDelay d;
            Task task;
            Moment start;
            using (d = new AsyncDisposableWithDelay(TimeSpan.FromMilliseconds(500))) {
                Assert.Equal(DisposalState.Active, d.DisposalState);
                Out.WriteLine("Active.");
                Out.WriteLine("Dispose started.");
                task = Task.Run(async () => {
                    await Task.Delay(100);
                    Assert.Equal(DisposalState.Disposing, d.DisposalState);
                    Out.WriteLine("Disposing check 2 passed.");
                });
                start = CpuClock.Now;
            }
            Assert.Equal(DisposalState.Disposing, d.DisposalState);
            Out.WriteLine("Disposing check 1 passed.");
            task.Wait();
            task = TestEx.WhenMet(
                () => d.DisposalState.Should().Be(DisposalState.Disposed),
                Intervals.Fixed(TimeSpan.FromMilliseconds(50)),
                d.DisposeDelay);
            task.Wait();
            Out.WriteLine("Dispose completed.");
        }
    }
}
