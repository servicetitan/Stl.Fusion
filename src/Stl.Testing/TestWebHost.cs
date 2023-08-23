using Microsoft.Extensions.Hosting;
using Stl.IO;
#if NETCOREAPP
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
#else
using Owin;
using System.Net.Http;
using System.Web.Http;
#endif

namespace Stl.Testing;

public interface ITestWebHost : IDisposable
{
    IHost Host { get; }
    IServiceProvider Services { get; }
    IServer Server { get; }
    ILoggerFactory LoggerFactory { get; }
    Uri ServerUri { get; }

    Task<IAsyncDisposable> Serve(bool disposeOnStop = true);
    HttpClient CreateClient();
}

public abstract class TestWebHostBase : ITestWebHost
{
    protected Lazy<IHost> HostLazy { get; set; }

    public IHost Host => HostLazy.Value;
    public IServiceProvider Services => Host.Services;
    public IServer Server => Services.GetRequiredService<IServer>();
    public ILoggerFactory LoggerFactory => Services.GetRequiredService<ILoggerFactory>();
    public Uri ServerUri { get; }

    protected TestWebHostBase(bool useHttps = false)
    {
        var localPort = WebTestExt.GetUnusedTcpPort();
        ServerUri = WebTestExt.GetLocalUri(localPort, useHttps ? "https" : "http");
        HostLazy = new Lazy<IHost>(CreateHost);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;

        if (HostLazy.IsValueCreated)
            Host.Dispose();
    }

    public async Task<IAsyncDisposable> Serve(bool disposeOnStop = true)
    {
        var host = Host;
        await host.StartAsync().ConfigureAwait(false);
        var addresses = Server.Features.Get<IServerAddressesFeature>();
        var serverUri = addresses?.Addresses?.FirstOrDefault();
        var log = LoggerFactory.CreateLogger(GetType());
        log.LogInformation("Serving @ {Uri}", serverUri);

        // ReSharper disable once HeapView.BoxingAllocation
        return AsyncDisposable.New(async self => {
            var host1 = self.Host;
            // 100ms for graceful shutdown
            using var cts = new CancellationTokenSource(100);
            await host1.StopAsync(cts.Token).SilentAwait(false);
            if (disposeOnStop)
                _ = Task.Run(() => host1.Dispose());
            self.HostLazy = new Lazy<IHost>(CreateHost);
        }, this);
    }

    public virtual HttpClient CreateClient()
        => new() { BaseAddress = ServerUri };

    protected virtual IHost CreateHost()
        => CreateHostBuilder().Build();

    protected virtual IHostBuilder CreateHostBuilder()
    {
        var wwwRootDir = FilePath.GetApplicationTempDirectory() & "tmp_wwwroot";
        Directory.CreateDirectory(wwwRootDir);

        var builder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder();
        builder.UseDefaultServiceProvider((ctx, options) => {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });

#if NETCOREAPP
        builder.ConfigureWebHost(webHost => {
            webHost.UseKestrel(kestrel => {
                kestrel.ConfigureEndpointDefaults(listen => {
                    listen.Protocols = HttpProtocols.Http1AndHttp2;
                    if (Equals(ServerUri.Scheme, "https"))
                        listen.UseHttps();
                });
            });
        webHost.UseSockets(socket => socket.NoDelay = true);
            webHost.UseKestrel();
            webHost.UseUrls(ServerUri.ToString());
            webHost.UseContentRoot(wwwRootDir);
            ConfigureWebHost(webHost);
        });
#endif

#if NETFRAMEWORK
        builder.ConfigureServices(
            (_, services) => {
                services.Configure<OwinWebApiServerOptions>(c => {
                    c.Urls = ServerUri.ToString();
                    c.ConfigureBuilder = ConfigureAppBuilder;
                    c.ConfigureHttp = ConfigureHttp;
                });
                services.AddHostedService<GenericWebHostService>();
                services.AddSingleton<IServer, OwinWebApiServer>();
            }
        );
#endif

        ConfigureHost(builder);
        return builder;
    }

    protected virtual void ConfigureHost(IHostBuilder builder) { }

#if NETCOREAPP
    protected virtual void ConfigureWebHost(IWebHostBuilder webHost) { }
#endif

#if NETFRAMEWORK
    protected virtual void ConfigureHttp(IServiceProvider services, HttpConfiguration config) { }

    protected virtual void ConfigureAppBuilder(IServiceProvider services, IAppBuilder builder) { }
#endif
}
