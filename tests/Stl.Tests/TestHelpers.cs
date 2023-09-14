using Stl.Generators;
using Stl.Rpc;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace Stl.Tests;

public static class TestHelpers
{
    public static Task Delay(double seconds, CancellationToken cancellationToken = default)
        => Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken);

    public static Task RandomDelay(double maxSeconds, CancellationToken cancellationToken = default)
    {
        var seconds = ConcurrentRandomDoubleGenerator.Default.Next() * maxSeconds;
        return Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken);
    }

    public static void GCCollect()
    {
        for (var i = 0; i < 3; i++) {
            GC.Collect();
            Thread.Sleep(10);
        }
    }

    public static IServiceProvider CreateLoggingServices(ITestOutputHelper @out)
    {
        var services = new ServiceCollection();
        services.AddLogging(logging => {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddDebug();
            logging.Services.AddSingleton<ILoggerProvider>(_ => {
#pragma warning disable CS0618
                return new XunitTestOutputLoggerProvider(
                    new TestOutputHelperAccessor() { Output = @out },
                    (_, level) => level >= LogLevel.Debug);
#pragma warning restore CS0618
            });
        });
        return services.BuildServiceProvider();
    }

    // Rpc

    public static Task AssertNoCalls(RpcPeer peer)
        => TestExt.WhenMet(() => {
            peer.OutboundCalls.Count.Should().Be(0);
            peer.InboundCalls.Count.Should().Be(0);
        }, TimeSpan.FromSeconds(1));

    public static Task AssertNoObjects(RpcPeer peer)
        => TestExt.WhenMet(() => {
            peer.RemoteObjects.Count.Should().Be(0);
            peer.SharedObjects.Count.Should().Be(0);
        }, TimeSpan.FromSeconds(1));
}
