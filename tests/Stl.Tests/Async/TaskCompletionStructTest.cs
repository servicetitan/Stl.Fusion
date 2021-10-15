namespace Stl.Tests.Async;

public class TaskCompletionStructTest : TestBase
{
    public TaskCompletionStructTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public void BasicTest()
    {
        var ts = TaskSource.New<int>(TaskCreationOptions.None);
        ts.Task.IsCompleted.Should().BeFalse();
        ts.SetResult(1);
        ts.TrySetResult(2).Should().BeFalse();
        ts.Task.IsCompletedSuccessfully().Should().BeTrue();
        ts.Task.Result.Should().Be(1);

        ts = TaskSource.New<int>(TaskCreationOptions.None);
        ts.Task.IsCompleted.Should().BeFalse();
        var e = new InvalidOperationException();
        ts.SetException(e);
        ts.TrySetException(new InvalidOperationException()).Should().BeFalse();
        ts.Task.IsCompleted.Should().BeTrue();
        ts.Task.IsFaulted.Should().BeTrue();
        ts.Task.Exception.Should().NotBeNull();

        ts = TaskSource.New<int>(TaskCreationOptions.None);
        ts.Task.IsCompleted.Should().BeFalse();
        using var cts = new CancellationTokenSource();
        ts.SetCanceled();
        ts.TrySetCanceled(cts.Token).Should().BeFalse();
        ts.Task.IsCompleted.Should().BeTrue();
        ts.Task.IsFaulted.Should().BeFalse();
        ts.Task.IsCanceled.Should().BeTrue();
    }
}
