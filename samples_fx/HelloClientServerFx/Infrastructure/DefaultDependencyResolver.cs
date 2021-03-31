using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace HelloClientServerFx
{
    internal class DefaultDependencyResolver : IDependencyResolver
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
            return services;
        }
        
        public void Dispose()
        {
        }

        public IDependencyScope BeginScope()
        {
            return this;
        }
    }
}