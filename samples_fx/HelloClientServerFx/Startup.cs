using System;
using System.Linq;
using System.Web.Http;
using Microsoft.Extensions.DependencyInjection;
using Owin;
using Stl.Fusion;
using Stl.Fusion.Server;

namespace HelloClientServerFx
{
    public class Startup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=316888
               
            // Configure Web API for self-host. 
            var config = new HttpConfiguration();
            Configure(config);

            var serviceProvider = config.DependencyResolver.GetService<IServiceProvider>();
            appBuilder.MapFusionWebSocketServer(serviceProvider);

            // run ensure initialized explicitly to see configuration errors;
            config.EnsureInitialized();

            appBuilder.UseWebApi(config); 
        }

        private void Configure(HttpConfiguration config)
        {
            config.AddDependencyResolver(Configure);
            
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new {id = RouteParameter.Optional}
            );
        }

        private void Configure(IServiceCollection services)
        {
            services.AddFusion(f => 
            {
                f.AddWebServer();
                f.AddComputeService<ICounterService, CounterService>();
            });
            
            //services.AddRouting();
            //services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());
            services.AddControllersAsServices(GetType().Assembly);
        }
    }
}