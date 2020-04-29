using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Async
{
    public class AsyncDisposableTest : TestBase
    {
        public class AsyncDisposableWithDelay : AsyncDisposableBase
        {
            public TimeSpan DisposeDelay { get; }

            public AsyncDisposableWithDelay(TimeSpan disposeDelay) => DisposeDelay = disposeDelay;

            protected override async ValueTask DisposeInternalAsync(bool disposing)
            {
                await Task.Delay(DisposeDelay); 
            } 
        }

        public AsyncDisposableTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task DisposeAsyncTest()
        {
            AsyncDisposableWithDelay? copy = null;
            var task = Task.Run(async () => {
                await Task.Delay(200);
                Assert.Equal(DisposalState.Disposing, copy!.DisposalState);
                Out.WriteLine("Disposing check passed.");
            });
            await using (var instance = new AsyncDisposableWithDelay(TimeSpan.FromMilliseconds(400))) {
                Assert.Equal(DisposalState.Active, instance.DisposalState);
                Out.WriteLine("Active.");
                copy = instance;
                Out.WriteLine("Dispose started.");
            }
            await task;
            Assert.Equal(DisposalState.Disposed, copy.DisposalState);
            Out.WriteLine("Dispose completed.");
        }

        [Fact]
        public void DisposeTest()
        {
            AsyncDisposableWithDelay? copy = null;
            var task = Task.Run(async () => {
                await Task.Delay(200);
                Assert.Equal(DisposalState.Disposing, copy!.DisposalState);
                Out.WriteLine("Disposing check 2 passed.");
            });
            using (var instance = new AsyncDisposableWithDelay(TimeSpan.FromMilliseconds(400))) {
                Assert.Equal(DisposalState.Active, instance.DisposalState);
                Out.WriteLine("Active.");
                copy = instance;
                Out.WriteLine("Dispose started.");
            }
            Assert.Equal(DisposalState.Disposing, copy.DisposalState);
            Out.WriteLine("Disposing check 1 passed.");
            task.Wait();
            Thread.Sleep(600);
            Assert.Equal(DisposalState.Disposed, copy.DisposalState);
            Out.WriteLine("Dispose completed.");
        }
    }
}
