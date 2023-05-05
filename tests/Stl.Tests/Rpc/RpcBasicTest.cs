using Stl.Rpc;

namespace Stl.Tests.Rpc;

public class RpcBasicTest : TestBase
{
    public RpcBasicTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public Task CallTest()
    {
        var services = CreateServerServices();
        return Task.CompletedTask;
    }

    private IServiceProvider CreateServerServices()
    {
        var services = new ServiceCollection();
        var rpc = services.AddRpc();
        rpc.HasService<ISimpleRpcService>().Serving<SimpleRpcService>();
        return services.BuildServiceProvider();
    }
}
