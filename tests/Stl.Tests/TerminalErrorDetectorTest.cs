namespace Stl.Tests;

public class TerminalErrorDetectorTest(ITestOutputHelper Out)
{
    private TerminalErrorDetector Detector = TerminalError.Detector;

    [Fact]
    public void CancellationTest()
    {
        Detector.Invoke(new OperationCanceledException(), default).Should().BeFalse();

        var error = (Exception?)null;
        var cts = new CancellationTokenSource();
        cts.Cancel();
        try {
            cts.Token.ThrowIfCancellationRequested();
        }
        catch (Exception e) {
            error = e;
        }
        error.Should().BeOfType<OperationCanceledException>();
        Detector.Invoke(error!, cts.Token).Should().BeTrue();
        Detector.Invoke(new AggregateException(error!), cts.Token).Should().BeFalse();
    }

    [Fact]
    public void ObjectDisposedTest()
    {
        Detector.Invoke(new ObjectDisposedException("Whatever"), default).Should().BeFalse();

        var services = new ServiceCollection()
            .AddSingleton<TerminalErrorDetectorTest>()
            .BuildServiceProvider();
        services.Dispose();
        var error = (Exception?)null;
        try {
            services.GetRequiredService<TerminalErrorDetectorTest>();
        }
        catch (Exception e) {
            error = e;
        }
        error.Should().BeOfType<ObjectDisposedException>();
        Detector.Invoke(error!, default).Should().BeTrue();
        Detector.Invoke(new AggregateException(error!), default).Should().BeTrue();
    }
}
