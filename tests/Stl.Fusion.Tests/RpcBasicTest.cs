using StackExchange.Redis;
using Stl.Fusion.Rpc;
using Stl.Fusion.Rpc.Interception;
using Stl.Fusion.Rpc.Internal;
using Stl.Fusion.Tests.Services;
using Stl.Interception;
using Stl.Rpc;
using Stl.Rpc.Infrastructure;
using Stl.Testing.Collections;

namespace Stl.Fusion.Tests;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class RpcBasicTest : SimpleFusionTestBase
{
    public RpcBasicTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public async Task BasicTest()
    {
        var services = CreateServices();
        var counters = services.GetRequiredService<ICounterService>();

        var c = Computed.GetExisting(() => counters.Get("a"));
        c.Should().BeNull();

        c = await Computed.Capture(() => counters.Get("a"));
        c.Value.Should().Be(0);
        var c1 = Computed.GetExisting(() => counters.Get("a"));
        c1.Should().BeSameAs(c);

        await counters.Increment("a");
        await TestExt.WhenMet(
            () => c.IsConsistent().Should().BeFalse(),
            TimeSpan.FromSeconds(1));
        c1 = Computed.GetExisting(() => counters.Get("a"));
        c1.Should().NotBeNull();
    }

    protected override void ConfigureServices(ServiceCollection services)
    {
        base.ConfigureServices(services);
        var fusion = services.AddFusion();
        fusion.AddComputeService<CounterService>();

        var fusionRpc = fusion.AddRpc();
        fusionRpc.AddRouter<ICounterService, CounterService>();
    }
}
