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
        services.AddFusion().AddComputeService<CounterService>();

        var rpc = services.AddRpc();
        rpc.Configuration.InboundCallTypes.Add(RpcComputeCall.CallTypeId, typeof(RpcInboundComputeCall<>));
        services.AddSingleton(_ => new RpcComputeServiceInterceptor.Options());
        services.AddSingleton(c => new RpcComputeServiceInterceptor(
            c.GetRequiredService<RpcComputeServiceInterceptor.Options>(), c));
        services.AddSingleton(c => (RpcComputedCache)new RpcNoComputedCache(c));
        if (!rpc.Configuration.Services.ContainsKey(typeof(IRpcComputeSystemCalls))) {
            rpc.AddService<IRpcComputeSystemCalls, RpcComputeSystemCalls>(RpcComputeSystemCalls.Name);
            services.AddSingleton(c => new RpcComputeSystemCalls(c));
            services.AddSingleton(c => new RpcComputeSystemCallSender(c));
        }

        rpc.AddService<ICounterService, CounterService>();
        var serviceType = typeof(ICounterService);
        var clientType = typeof(ICounterService);
        var serverType = typeof(CounterService);
        services.AddSingleton(clientType, c => {
            var rpcHub = c.RpcHub();
            var server = c.GetRequiredService(serverType);
            var rpcClient = rpcHub.CreateClient(clientType);

            var computeServiceInterceptor = c.GetRequiredService<RpcComputeServiceInterceptor>();
            var clientProxy = Proxies.New(clientType, computeServiceInterceptor, rpcClient);

            var routingInterceptor = c.GetRequiredService<RpcRoutingInterceptor>();
            var serviceDef = rpcHub.ServiceRegistry[serviceType];
            routingInterceptor.Setup(serviceDef, server, clientProxy);
            var routingProxy = Proxies.New(clientType, routingInterceptor);
            return routingProxy;
        });
    }
}
