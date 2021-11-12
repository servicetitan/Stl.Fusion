namespace Samples.HelloCart.V1;

public class AppV1 : AppBase
{
    public AppV1()
    {
        var services = new ServiceCollection();
        services.AddFusion(fusion => {
            fusion.AddComputeService<IProductService, InMemoryProductService>();
            fusion.AddComputeService<ICartService, InMemoryCartService>();
        });
        ClientServices = HostServices = services.BuildServiceProvider();
    }
}
