using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Stl.OS;
using Stl.DependencyInjection;

namespace Templates.TodoApp.UI;

public class Program
{
    public static Task Main(string[] args)
    {
        if (OSInfo.Kind != OSKind.WebAssembly)
            throw new ApplicationException("This app runs only in browser.");

        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        StartupHelper.ConfigureServices(builder.Services, builder);
        var host = builder.Build();
        host.Services.HostedServices().Start();
        return host.RunAsync();
    }
}
