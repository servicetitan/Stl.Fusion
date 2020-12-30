using Microsoft.Extensions.DependencyInjection;

namespace Stl.DependencyInjection
{
    public interface IModule
    {
        IServiceCollection Services { get; set; }
        void ConfigureServices();
    }
}
