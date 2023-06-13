using Stl.Tests.Rpc;

namespace Stl.Fusion.Tests;

public abstract class SimpleFusionTestBase : RpcLocalTestBase
{
    protected SimpleFusionTestBase(ITestOutputHelper @out) : base(@out) { }

    protected override void ConfigureServices(ServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddFusion();
    }

    protected IServiceProvider CreateServicesWithComputeService<TService>()
        where TService : class, IComputeService
        => CreateServices(
            services => services.AddFusion().AddService<TService>());
}
