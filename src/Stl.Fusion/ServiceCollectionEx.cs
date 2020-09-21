using Microsoft.Extensions.DependencyInjection;

namespace Stl.Fusion
{
    public static class ServiceCollectionEx
    {
        public static FusionBuilder AddFusion(this IServiceCollection services)
            => new FusionBuilder(services);
    }
}
