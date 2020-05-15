using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Red;
using Stl.IO;
using Stl.OS;

namespace Stl.Samples.Blazor
{
    public class Program
    {
        public static Task Main(string[] args) 
            => OSInfo.Kind == OSKind.Wasm 
                ? RunClientAsync(args) 
                : RunServerAsync(args);

        private static Task RunClientAsync(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.Configuration.Add(new MemoryConfigurationSource() {
                InitialData = new Dictionary<string, string>() {
                    { "ASPNETCORE_ENVIRONMENT", "Development" },
                } 
            });
            builder.RootComponents.Add<App>("app");
            builder.Services.AddTransient(sp => new HttpClient {
                BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
            });
            var host = builder.Build();
            return host.RunAsync();
        }

        private static Task RunServerAsync(string[] args)
        {
            var myLocation = (PathString) Assembly.GetExecutingAssembly().Location;
            var baseDir = myLocation.GetDirectoryPath();
            var wwwRoot = baseDir & "wwwroot";
            var server = new RedHttpServer();
            server.ConfigureApplication = app => {
                var wwwRootFileProvider = new PhysicalFileProvider(wwwRoot);
                var contentTypeProvider = new FileExtensionContentTypeProvider();
                app.UseDefaultFiles();
                app.UseStaticFiles(new StaticFileOptions {
                    FileProvider = wwwRootFileProvider,
                    ContentTypeProvider = contentTypeProvider,
                    DefaultContentType = "application/octet-stream",
                    ServeUnknownFileTypes = true,
                });
            };
            return server.RunAsync();
        }
    }
}
