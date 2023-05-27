using StackExchange.Redis;
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
        return;
        var services = CreateServices();
        var counters = services.GetRequiredService<ICounterService>();

        var c = Computed.GetExisting(() => counters.Get("a"));
        c.Should().BeNull();

        c = await Computed.Capture(() => counters.Get("a"));
        c.Value.Should().Be(0);
        var c1 = Computed.GetExisting(() => counters.Get("a"));
        c1.Should().BeSameAs(c);

        await counters.Increment("a");
        c.IsConsistent().Should().BeFalse();
        c1 = Computed.GetExisting(() => counters.Get("a"));
        c1.Should().BeNull();
    }

    protected override void ConfigureServices(ServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddFusion().AddComputeService<CounterService>();

        var rpc = services.AddRpc();
        rpc.Configuration.InboundCallTypes.Add(RpcComputeCall.CallTypeId, typeof(RpcInboundComputeCall<>));
        services.AddSingleton(_ => new RpcComputeMethodInterceptor.Options());
        services.AddSingleton(c => new RpcComputeMethodInterceptor(
            c.GetRequiredService<RpcComputeMethodInterceptor.Options>(), c));

        rpc.AddService<ICounterService, CounterService>();
        var clientType = typeof(ICounterServiceClient);
        services.AddSingleton(clientType, c => {
            var serviceType = typeof(ICounterService);
            var serviceRegistry = c.RpcHub().ServiceRegistry;

            var server = c.GetRequiredService(serviceType);
            var client = c.GetRequiredService(clientType); // Replace it with actual client
            var interceptor = c.GetRequiredService<RpcRoutingInterceptor>();
            interceptor.Configure(serviceRegistry[serviceType], server, client); 
            var computeServiceProxy = Proxies.New(clientType, interceptor);
            return computeServiceProxy;
        });
    }
}
