using System.Collections.Generic;
using System.CommandLine;
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
using static Microsoft.Extensions.Hosting.Host;

[assembly: Plugin(typeof(PrimaryHostPlugin))]

namespace Stl.Hosting.Plugins 
{
    public interface IPrimaryHostPlugin : ISingletonPlugin
    {
        IHost BuildHost(string[] hostArguments);
    }

    public class PrimaryHostPlugin : IPrimaryHostPlugin, IAppHostBuilderPlugin
    {
        protected IPluginHost Plugins { get; set; } = null!;
        protected IAppHostBuilder AppHostBuilder { get; set; } = null!;
        protected string[] HostArguments { get; set; } = {};
        protected IHostBuilder HostBuilder { get; set; } = null!;

        public PrimaryHostPlugin() { }
        public PrimaryHostPlugin(IPluginHost plugins, IAppHostBuilder appHostBuilder)
        {
            Plugins = plugins;
            AppHostBuilder = appHostBuilder;
        }

        public virtual IHost BuildHost(string[] hostArguments)
        {
            HostArguments = hostArguments;
            HostBuilder = CreateHostBuilder();
            ConfigureServiceProviderFactory();
            ConfigureHostConfiguration();
            ConfigureServices();
            ConfigureWebHost();
            UseHostPlugins();
            return HostBuilder.Build();
        }

        protected virtual IHostBuilder CreateHostBuilder()
            => CreateDefaultBuilder(HostArguments);

        protected virtual void ConfigureServiceProviderFactory() 
            => HostBuilder.UseServiceProviderFactory(new AutofacServiceProviderFactory());

        protected virtual void ConfigureHostConfiguration()
            => HostBuilder.ConfigureHostConfiguration(config => {
                config
                    .SetBasePath(AppHostBuilder.BaseDirectory)
                    .AddEnvironmentVariables(AppHostBuilder.EnvironmentVarPrefix);
                foreach (var fileName in GetHostConfigurationFileNames())
                    config.AddFile(fileName);
            });

        protected virtual IEnumerable<string> GetHostConfigurationFileNames()
        {
            yield return "settings.json";
            yield return $"settings.{AppHostBuilder.BuildState.Environment}.json"; 
        }

        protected virtual void ConfigureServices()
            => HostBuilder.ConfigureServices((ctx, services) => {
                services.AddOptions();
                services.AddSingleton(Plugins);
                services.CopySingleton<IAppHostBuilder>(Plugins);
                services.CopySingleton<IConsole>(Plugins);
                services.AddLogging(logging => ConfigureLogging(ctx, logging));
                services.AddControllersWithViews();
            });

        protected virtual void ConfigureLogging(HostBuilderContext ctx, ILoggingBuilder logging)
        {
            var cfg = ctx.Configuration;
            var section = cfg.GetSection(AppHostBuilder.LoggingSectionName);
            logging.AddConfiguration(section);
            logging.AddConsole();
            logging.AddDebug();
            logging.AddEventSourceLogger();
        }

        protected virtual void ConfigureWebHost() 
            => HostBuilder.ConfigureWebHostDefaults(ConfigureWebHost);

        protected virtual void ConfigureWebHost(IWebHostBuilder webHostBuilder)
        {
            ConfigureWebServer(webHostBuilder);
            webHostBuilder.Configure(ConfigureWebApp);
            UseWebHostPlugins(webHostBuilder);
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
