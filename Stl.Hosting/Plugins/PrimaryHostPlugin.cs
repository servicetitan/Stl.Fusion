using System.Collections.Generic;
using System.CommandLine;
using System.Reactive.PlatformServices;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stl.Extensibility;
using Stl.Hosting.Plugins;
using Stl.Plugins;
using Stl.Plugins.Extensions.Hosting;
using Stl.Plugins.Extensions.Web;
using Stl.Time;
using static Microsoft.Extensions.Hosting.Host;

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
            => CreateDefaultBuilder(AppHostBuilder.BuildState.HostArguments.ToArray());

        protected virtual void ConfigureServiceProviderFactory() 
            => HostBuilder.UseServiceProviderFactory(new AutofacServiceProviderFactory());

        protected virtual void ConfigureHostConfiguration()
            => HostBuilder.ConfigureHostConfiguration(config => {
                config
                    .SetBasePath(AppHostBuilder.BaseDirectory)
                    .AddEnvironmentVariables(AppHostBuilder.EnvironmentVarPrefix);
                if (AppHostBuilder.IsTestHost)
                    return;
                foreach (var fileName in GetHostConfigurationFileNames())
                    config.AddFile(fileName);
            });

        protected virtual IEnumerable<string> GetHostConfigurationFileNames()
        {
            yield return "settings.json";
            yield return $"settings.{AppHostBuilder.BuildState.EnvironmentName}.json"; 
        }

        protected virtual void ConfigureServices()
            => HostBuilder.ConfigureServices((ctx, services) => {
                var env = ctx.HostingEnvironment;
                // We have to update EnvironmentName here, since earlier we didn't read
                // exactly the same configuration (+ command line options weren't parsed yet).
                AppHostBuilder.BuildState.EnvironmentName = env.EnvironmentName;
                services.AddOptions();
                services.AddSingleton(Plugins);
                services.CopySingleton<IAppHostBuilder>(Plugins);
                services.CopySingleton<IClock>(Plugins);
                services.CopySingleton<ISystemClock>(Plugins);
                services.CopySingleton<Microsoft.Extensions.Internal.ISystemClock>(Plugins);
                services.CopySingleton<IConsole>(Plugins);
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
            => HostBuilder.ConfigureWebHostDefaults(ConfigureWebHost);

        protected virtual void ConfigureWebHost(IWebHostBuilder webHostBuilder)
        {
            TestAppHostBuilder?.Implementation?.InvokePreBuilders(webHostBuilder);
            ConfigureWebServer(webHostBuilder);
            webHostBuilder.Configure(ConfigureWebApp);
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
