using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.StaticFiles.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Red;
using Stl.IO;
using Stl.OS;

namespace Stl.Samples.Blazor
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            switch (OSInfo.Kind) {
            case OSKind.Wasm:
                var builder = WebAssemblyHostBuilder.CreateDefault(args);
                builder.RootComponents.Add<App>("app");
                builder.Services.AddTransient(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
                await builder.Build().RunAsync();
                break;
            default:
                var myLocation = (PathString) Assembly.GetExecutingAssembly().Location;
                var baseDir = myLocation.GetDirectoryPath();
                var wwwRoot = baseDir & "wwwroot";
                var server = new RedHttpServer();
                server.ConfigureApplication = app => {
                    var wwwRootFileProvider = new PhysicalFileProvider(wwwRoot);
                    var contentTypeProvider = new FileExtensionContentTypeProvider();
                    app.UseStaticFiles(new StaticFileOptions {
                        FileProvider = wwwRootFileProvider,
                        ContentTypeProvider = contentTypeProvider,
                        DefaultContentType = "application/octet-stream",
                        ServeUnknownFileTypes = true,
                    });
                    app.UseDefaultFiles();
                };
                await server.RunAsync();
                break;
            } 
        }
    }
}
