using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stl.Async;
using Stl.IO;

namespace Stl.Testing
{
    public interface ITestWebHost : IDisposable
    {
        IHost Host { get; }
        IServiceProvider Services { get; }
        IServer Server { get; }
        Uri ServerUri { get; }

        Task<IAsyncDisposable> ServeAsync();
        HttpClient CreateClient();
    }

    public abstract class TestWebHostBase : ITestWebHost
    {
        protected Lazy<IHost> HostLazy { get; set; }
        protected Lazy<Uri> ServerUriLazy { get; set; }

        public IHost Host => HostLazy.Value;
        public IServiceProvider Services => Host.Services;
        public IServer Server => Services.GetRequiredService<IServer>();
        public Uri ServerUri => ServerUriLazy.Value;

        protected TestWebHostBase()
        {
            HostLazy = new Lazy<IHost>(CreateHost);
            ServerUriLazy = new Lazy<Uri>(() => {
                var addresses = Server.Features.Get<IServerAddressesFeature>();
                return new Uri(addresses.Addresses.First());
            });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (HostLazy.IsValueCreated)
                Host.Dispose();
        }

        public async Task<IAsyncDisposable> ServeAsync()
        {
            var host = Host;
            await host.StartAsync().ConfigureAwait(false);
            // ReSharper disable once HeapView.BoxingAllocation
            return AsyncDisposable.New(async self => {
                var host1 = self.Host;
                await host1.StopAsync().SuppressExceptions().ConfigureAwait(false);
                host1.Dispose();
                self.HostLazy = new Lazy<IHost>(CreateHost);
            }, this);
        }

        public virtual HttpClient CreateClient()
            => new HttpClient() { BaseAddress = ServerUri };

        protected virtual IHost CreateHost()
            => CreateHostBuilder().Build();

        protected virtual IHostBuilder CreateHostBuilder()
        {
            var emptyDir = PathEx.GetApplicationDirectory() & "Empty";
            Directory.CreateDirectory(emptyDir);

            var builder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder();
            builder.ConfigureWebHost(b => {
                var serverUri = ServerUriLazy.IsValueCreated
                    ? ServerUri.ToString()
                    : "http://127.0.0.1:0";
                b.UseKestrel();
                b.UseUrls(serverUri);
                b.UseContentRoot(emptyDir);
                ConfigureWebHost(b);
            });
            ConfigureHost(builder);
            return builder;
        }

        protected virtual void ConfigureHost(IHostBuilder builder) { }
        protected virtual void ConfigureWebHost(IWebHostBuilder builder) { }
    }
}
