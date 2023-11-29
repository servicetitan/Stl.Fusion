namespace Stl.Tests.Async;

public class CancellationTokenSourceExtTest
{
    [Fact]
    public void IsDisposedTest()
    {
        var cts = new CancellationTokenSource();
        cts.IsDisposed().Should().BeFalse();
        cts.Dispose();
        cts.IsDisposed().Should().BeTrue();
    }
}
