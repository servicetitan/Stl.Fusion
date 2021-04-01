using System;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin;
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

            appBuilder.UseWebApi(config); 
        }

        private void Configure(HttpConfiguration config)
        {
            var serviceProvider = CreateServiceProvider();

            config.DependencyResolver = new DefaultDependencyResolver(serviceProvider);
            
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new {id = RouteParameter.Optional}
            );
            
            
        }

        private ServiceProvider CreateServiceProvider()
        {
            IServiceCollection services = new ServiceCollection();
            Configure(services);
            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider;
        }

        private void AddControllersAsServices(IServiceCollection services)
        {
            services.AddControllersAsServices(GetType().Assembly.GetExportedTypes()
                .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
                .Where(t => typeof(IHttpController).IsAssignableFrom(t)
                            || t.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase)));
        }

        private void Configure(IServiceCollection services)
        {
            AddControllersAsServices(services);

            services.AddFusion(f =>
            {
                f.AddWebServer();
                f.AddComputeService<ICounterService, CounterService>();
            });
            //services.AddRouting();
            //services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());
        }
    }
}