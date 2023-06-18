using Stl.Rpc;

namespace Stl.Tests;

public static class TestHelpers
{
    public static Task Delay(double seconds, CancellationToken cancellationToken = default)
        => Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken);

    public static void GCCollect()
    {
        for (var i = 0; i < 3; i++) {
            GC.Collect();
            Thread.Sleep(10);
        }
    }

    // Rpc

    public static Task AssertNoCalls(RpcPeer peer)
        => TestExt.WhenMet(() => {
            peer.OutboundCalls.Count.Should().Be(0);
            peer.InboundCalls.Count.Should().Be(0);
        }, TimeSpan.FromSeconds(0.5));
}
