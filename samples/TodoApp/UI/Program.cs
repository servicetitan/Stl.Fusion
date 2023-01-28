using System.Globalization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Stl.DependencyInjection;

namespace Templates.TodoApp.UI;

public class Program
{
    public static Task Main(string[] args)
    {
        var culture = CultureInfo.CreateSpecificCulture("fr-FR");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        ComputedRegistry.Instance.OnRegister = c => Console.WriteLine($"+ {c.Input.Category}");
        ComputedRegistry.Instance.OnUnregister = c => Console.WriteLine($"- {c.Input.Category}");

        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        StartupHelper.ConfigureServices(builder.Services, builder);
        var host = builder.Build();
        _ = host.Services.HostedServices().Start();
        return host.RunAsync();
    }
}
