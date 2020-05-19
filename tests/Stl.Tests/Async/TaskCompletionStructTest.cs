using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Stl.Async;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Async
{
    public class TaskCompletionStructTest : TestBase
    {
        public TaskCompletionStructTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public void BasicTest()
        {
            var tcs = new TaskCompletionStruct<int>(TaskCreationOptions.None);
            tcs.Task.IsCompleted.Should().BeFalse();
            tcs.SetResult(1);
            tcs.TrySetResult(2).Should().BeFalse();
            tcs.Task.IsCompletedSuccessfully.Should().BeTrue();
            tcs.Task.Result.Should().Be(1);

            tcs = new TaskCompletionStruct<int>(TaskCreationOptions.None);
            tcs.Task.IsCompleted.Should().BeFalse();
            var e = new InvalidOperationException();
            tcs.SetException(e);
            tcs.TrySetException(new InvalidOperationException()).Should().BeFalse();
            tcs.Task.IsCompleted.Should().BeTrue();
            tcs.Task.IsFaulted.Should().BeTrue();
            tcs.Task.Exception.Should().NotBeNull();

            tcs = new TaskCompletionStruct<int>(TaskCreationOptions.None);
            tcs.Task.IsCompleted.Should().BeFalse();
            using var cts = new CancellationTokenSource(); 
            tcs.SetCanceled();
            tcs.TrySetCanceled(cts.Token).Should().BeFalse();
            tcs.Task.IsCompleted.Should().BeTrue();
            tcs.Task.IsFaulted.Should().BeFalse();
            tcs.Task.IsCanceled.Should().BeTrue();
        }
    }
}
