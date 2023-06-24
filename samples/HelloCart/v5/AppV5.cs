using Samples.HelloCart.V4;

namespace Samples.HelloCart.V5;

public class AppV5 : AppV4
{
    public WebApplication ExtraApp { get; }
    // We'll modify the data on AppHost & watch it changes on App
    public override IServiceProvider WatchedServices => ServerServices;

    public AppV5()
    {
        // Server 1
        App = CreateWebApp("http://localhost:7005");
        ServerServices = App.Services;

        // Server 2
        var extraAppUri = "http://localhost:7006";
        ExtraApp = CreateWebApp(extraAppUri);

        // Client
        ClientServices = BuildClientServices(extraAppUri);
    }

    public override async Task InitializeAsync(IServiceProvider services)
    {
        await base.InitializeAsync(services);
        await ExtraApp.StartAsync();
        await Task.Delay(100);
    }

    public override async ValueTask DisposeAsync()
    {
        if (ClientServices is IAsyncDisposable csd)
            await csd.DisposeAsync();

        await ExtraApp.StopAsync();
        await ExtraApp.DisposeAsync();

        await App.StopAsync();
        await App.DisposeAsync();
    }
}
