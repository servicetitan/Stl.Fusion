using Samples.HelloCart.V4;

namespace Samples.HelloCart.V5;

public class AppV5 : AppV4
{
    public IHost ExtraHost { get; protected set; }
    // We'll modify the data on ExtraHost & watch on Host
    public override IServiceProvider WatchServices => HostServices;

    public AppV5()
    {
        var hostUri = new Uri("http://localhost:7005");
        Host = BuildHost(hostUri);
        HostServices = Host.Services;

        var extraHostUri = new Uri("http://localhost:7006");
        ExtraHost = BuildHost(extraHostUri);
        ClientServices = BuildClientServices(extraHostUri);
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ExtraHost.StartAsync();
        await Task.Delay(100);
    }

    public override async ValueTask DisposeAsync()
    {
        if (ClientServices is IAsyncDisposable csd)
            await csd.DisposeAsync();
        await ExtraHost.StopAsync();
        ExtraHost.Dispose();
        await Host.StopAsync();
        Host.Dispose();
    }
}
