using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stl.Collections;
using Stl.Fusion.Server;
using Stl.Testing;
#if NETCOREAPP
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
#else
using Owin;
using System.Web.Http;
#endif

namespace Stl.Fusion.Tests.Services
{
    public class FusionTestWebHostOptions
    {
#if NET471
        public Type[]? ControllerTypes { get; set; }
#endif
    }
    
    public class FusionTestWebHost : TestWebHostBase
    {
        public IServiceCollection BaseServices { get; }
        public FusionTestWebHostOptions Options { get; }
        
        public FusionTestWebHost(IServiceCollection baseServices, FusionTestWebHostOptions options)
        {
            BaseServices = baseServices;
            Options = options;
        }

#if NETCOREAPP

        public FusionTestWebHost(IServiceCollection baseServices)
            => BaseServices = baseServices;

        protected override void ConfigureHost(IHostBuilder builder)
        {
            builder.ConfigureServices(services => {
                // Copy all services from the base service provider here
                services.AddRange(BaseServices);

                // Since we copy all services here,
                // only web-related ones must be added to services
                services.AddFusion(fusion => {
                    fusion.AddWebServer();
                    fusion.AddAuthentication(auth => auth.AddServer());
                });

                // Web
                services.AddRouting();
                services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());

                // Testing
                services.AddHostedService<ApplicationPartsLogger>();
            });
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.Configure((ctx, app) => {
                app.UseWebSockets();
                app.UseFusionSession();

                // API controllers
                app.UseRouting();
                app.UseEndpoints(endpoints => {
                    endpoints.MapControllers();
                    endpoints.MapFusionWebSocketServer();
                });
            });
        }
#endif

#if NET461_OR_GREATER
        protected override void ConfigureHost(IHostBuilder builder)
        {
            builder.ConfigureServices(services => {
                // Copy all services from the base service provider here
                services.AddRange(BaseServices);

                // Since we copy all services here,
                // only web-related ones must be added to services
                services.AddFusion(fusion => {
                    fusion.AddWebServer();
                    // TODO: restore later
                    //fusion.AddAuthentication(auth => auth.AddServer());
                });

                if (Options.ControllerTypes!=null)
                    services.AddControllersAsServices(Options.ControllerTypes);

                // TODO: restore later
                //// Web
                //services.AddRouting();
                //services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());

                //// Testing
                //services.AddHostedService<ApplicationPartsLogger>();
            });
        }

        protected override void SetupHttpConfiguration(IServiceProvider svp, HttpConfiguration config)
        {
            base.SetupHttpConfiguration(svp, config);
            
            config.Formatters.Insert(0, new TextMediaTypeFormatter());
        }

        protected override void ConfigureWebHost(IServiceProvider svp, IAppBuilder builder)
        {
            base.ConfigureWebHost(svp, builder);

            builder.MapFusionWebSocketServer(svp);
        }

        // TODO: restore later with using IAppBuilder
        //protected override void ConfigureWebHost(IWebHostBuilder builder)
        //{
        //    builder.Configure((ctx, app) => {
        //        app.UseWebSockets();
        //        app.UseFusionSession();

        //        // API controllers
        //        app.UseRouting();
        //        app.UseEndpoints(endpoints => {
        //            endpoints.MapControllers();
        //            endpoints.MapFusionWebSocketServer();
        //        });
        //    });
        //}
#endif
    }
}
