#if NETFRAMEWORK

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Owin.Hosting;
using Owin;

namespace Stl.Testing;

public class OwinWebApiServerOptions
{
    public string Urls { get; set; } = null!;
    public Action<IServiceProvider,IAppBuilder> ConfigureBuilder { get; set; } = null!;
    public Action<IServiceProvider,HttpConfiguration> SetupHttpConfiguration { get; set; } = null!;
}

internal class OwinWebApiServer : IServer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ServerAddressesFeature _serverAddresses;
    private bool _hasStarted;
#pragma warning disable 169
    private int _stopping;
#pragma warning restore 169

    private readonly OwinWebApiServerOptions options;
    //private readonly CancellationTokenSource _stopCts = new CancellationTokenSource();
    //private readonly TaskCompletionSource _stoppedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    private IDisposable _application = null!;

    public IFeatureCollection Features { get; }

    public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
    {
        if (_hasStarted)
            throw new InvalidOperationException("The server has already started and/or has not been cleaned up yet");
        _hasStarted = true;

        string baseAddress = options.Urls;
        Action<IAppBuilder> configureBuilder = (appBuilder) => options.ConfigureBuilder(_serviceProvider, appBuilder);
        Action<HttpConfiguration> setupConfiguration = (config) => options.SetupHttpConfiguration(_serviceProvider, config);
        _application = WebApp.Start(baseAddress, new WebApiStartup(_serviceProvider, setupConfiguration, configureBuilder).Configuration);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _application?.Dispose();
        _application = null!;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        StopAsync(new CancellationToken(canceled: true)).GetAwaiter().GetResult();
    }

    public OwinWebApiServer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        options = serviceProvider.GetRequiredService<IOptions<OwinWebApiServerOptions>>().Value;
        Features = new FeatureCollection();
        _serverAddresses = new ServerAddressesFeature();
        Features.Set<IServerAddressesFeature>(_serverAddresses);
        _serverAddresses.Addresses.Add(options.Urls);
    }
}

internal class WebApiStartup
{
    private readonly IServiceProvider serviceProvider;
    private readonly Action<HttpConfiguration> _setupConfiguration;
    private readonly Action<IAppBuilder> _configureAppBuilder;

    public WebApiStartup(IServiceProvider serviceProvider, Action<HttpConfiguration> setupConfiguration,
        Action<IAppBuilder> configureAppBuilder)
    {
        this.serviceProvider = serviceProvider;
        _setupConfiguration = setupConfiguration;
        _configureAppBuilder = configureAppBuilder;
    }

    public void Configuration(IAppBuilder appBuilder)
    {
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=316888

        // Configure Web API for self-host.
        var config = new HttpConfiguration();
        Configure(config);

        if (_configureAppBuilder != null)
            _configureAppBuilder(appBuilder);
        var appBuilders = serviceProvider.GetServices<Action<IAppBuilder>>();
        foreach (var service in appBuilders) {
            service(appBuilder);
        }

        appBuilder.UseWebApi(config);
    }

    private void Configure(HttpConfiguration config)
    {
        if (_setupConfiguration != null)
            _setupConfiguration(config);
        var configBuilders = serviceProvider.GetServices<Action<HttpConfiguration>>();
        foreach (var service in configBuilders) {
            service(config);
        }
        config.DependencyResolver = new DefaultDependencyResolver(serviceProvider);

        config.MapHttpAttributeRoutes();

        config.Routes.MapHttpRoute(
            name: "DefaultApi",
            routeTemplate: "api/{controller}/{action}"
        );
    }
}

public class DefaultDependencyResolver : IDependencyResolver
{
    private IServiceProvider serviceProvider;

    public DefaultDependencyResolver(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public object GetService(Type serviceType)
    {
        var service = this.serviceProvider.GetService(serviceType);
        return service;
    }

    public IEnumerable<object> GetServices(Type serviceType)
    {
        var services = this.serviceProvider.GetServices(serviceType);
        return services!;
    }

    public void Dispose()
    {
    }

    public IDependencyScope BeginScope()
    {
        return this;
    }
}

internal class GenericWebHostService : IHostedService
{
    private readonly IServer server;

    public GenericWebHostService(IServer server) => this.server = server;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await server.StartAsync(new HostingApplication(), cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await server.StopAsync(cancellationToken);
    }
}

#endif
