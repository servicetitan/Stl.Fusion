using System.Collections.Generic;
using System.CommandLine;
using System.Reactive;
using System.Reactive.PlatformServices;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stl.Extensibility;
using Stl.Hosting.Plugins;
using Stl.Plugins;
using Stl.Plugins.Extensions.Hosting;
using Stl.Plugins.Extensions.Web;
using Stl.Time;

[assembly: Plugin(typeof(PrimaryHostPlugin))]

namespace Stl.Hosting.Plugins
{
    public interface IPrimaryHostPlugin : ISingletonPlugin
    {
        IHost BuildHost();
    }

    public class PrimaryHostPlugin : IPrimaryHostPlugin, IAppHostBuilderPlugin
    {
        protected IPluginHost Plugins { get; set; } = null!;
        protected IAppHostBuilder AppHostBuilder { get; set; } = null!;
        protected ITestAppHostBuilder? TestAppHostBuilder =>
            AppHostBuilder.IsTestHost ? (ITestAppHostBuilder) AppHostBuilder : null;
        protected IHostBuilder HostBuilder { get; set; } = null!;

        public PrimaryHostPlugin() { }
        public PrimaryHostPlugin(IPluginHost plugins, IAppHostBuilder appHostBuilder)
        {
            Plugins = plugins;
            AppHostBuilder = appHostBuilder;
        }

        public virtual IHost BuildHost()
        {
            HostBuilder = CreateHostBuilder();
            ConfigureServiceProviderFactory();
            ConfigureHostConfiguration();
            TestAppHostBuilder?.Implementation?.InvokePreBuilders(HostBuilder);
            ConfigureServices();
            ConfigureWebHost();
            UseHostPlugins();
            TestAppHostBuilder?.Implementation?.InvokePostBuilders(HostBuilder);
            return HostBuilder.Build();
        }

        protected virtual IHostBuilder CreateHostBuilder()
            => Host.CreateDefaultBuilder(AppHostBuilder.BuildState.HostArguments.ToArray());

        protected virtual void ConfigureServiceProviderFactory()
            => HostBuilder.UseServiceProviderFactory(new AutofacServiceProviderFactory());

        protected virtual void ConfigureHostConfiguration()
            => HostBuilder.ConfigureHostConfiguration(cfg => {
                var b = AppHostBuilder;
                cfg.SetBasePath(b.BaseDirectory);
                cfg.AddInMemoryCollection(new [] {
                    // That's to make sure environment name isn't overriden by any of
                    // configuration sources provided further.
                    KeyValuePair.Create(HostDefaults.EnvironmentKey, b.BuildState.EnvironmentName),
                });
                cfg.AddEnvironmentVariables($"{b.EnvironmentVarPrefix}{b.BuildState.EnvironmentName}_");
                cfg.AddEnvironmentVariables(b.EnvironmentVarPrefix);
                if (!b.IsTestHost)
                    foreach (var fileName in GetHostConfigurationFileNames())
                        cfg.AddFile(fileName);
            });

        protected virtual IEnumerable<string> GetHostConfigurationFileNames()
        {
            yield return "settings.json";
            yield return $"settings.{AppHostBuilder.BuildState.EnvironmentName}.json";
        }

        protected virtual void ConfigureServices()
            => HostBuilder.ConfigureServices((ctx, services) => {
                services.AddOptions();
                services.TryAddSingleton(Plugins);
                services.TryCopySingleton<IAppHostBuilder>(Plugins);
                services.TryCopySingleton<ISectionRegistry>(Plugins);
                services.TryCopySingleton<IMomentClock>(Plugins);
                services.TryCopySingleton<ISystemClock>(Plugins);
                services.TryCopySingleton<Microsoft.Extensions.Internal.ISystemClock>(Plugins);
                services.TryCopySingleton<IConsole>(Plugins);
                services.AddLogging(logging => ConfigureLogging(ctx, logging));
                services.AddControllersWithViews();
            });

        protected virtual void ConfigureLogging(HostBuilderContext ctx, ILoggingBuilder logging)
        {
            var cfg = ctx.Configuration;
            var section = cfg.GetSection(AppHostBuilder.LoggingSectionName);
            if (AppHostBuilder.IsTestHost) {
                logging.AddDebug();
                return;
            }
            logging.AddConfiguration(section);
            logging.AddConsole();
            logging.AddDebug();
            logging.AddEventSourceLogger();
        }

        protected virtual void ConfigureWebHost()
            => HostBuilder.ConfigureWebHost(ConfigureWebHost);

        protected virtual void ConfigureWebHost(IWebHostBuilder webHostBuilder)
        {
            TestAppHostBuilder?.Implementation?.InvokePreBuilders(webHostBuilder);
            ConfigureWebServer(webHostBuilder);
            webHostBuilder.Configure((context, builder) => {
                ConfigureWebApp(context, builder);
                AppHostBuilder.UsePlugins<IConfigureWebAppPlugin>(context, builder, Plugins);
            });
            UseWebHostPlugins(webHostBuilder);
            TestAppHostBuilder?.Implementation?.InvokePostBuilders(webHostBuilder);
        }

        protected virtual void ConfigureWebServer(IWebHostBuilder webHostBuilder)
            => webHostBuilder
                .UseKestrel()
                .UseUrls(AppHostBuilder.WebHostUrls.ToArray());

        protected virtual void ConfigureWebApp(WebHostBuilderContext ctx, IApplicationBuilder app)
        {
            var env = ctx.HostingEnvironment;
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else {
                app.UseExceptionHandler("/Error/");
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();
            // app.UseMvcWithDefaultRoute();
            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });
        }

        protected virtual void UseHostPlugins()
            => HostBuilder.UsePlugins<IHostPlugin>(Plugins);

        protected virtual void UseWebHostPlugins(IWebHostBuilder webHostBuilder)
            => webHostBuilder.UsePlugins<IWebHostPlugin>(Plugins);
    }
}
