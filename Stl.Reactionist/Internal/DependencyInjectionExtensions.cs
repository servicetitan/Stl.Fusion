using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Reactionist.Internal
{
    public static class DependencyInjectionExtensions
    {
        public static bool HasService<TService>(this IServiceCollection services) => 
            services.Any(d => d.ServiceType == typeof(TService));            
    }
}
