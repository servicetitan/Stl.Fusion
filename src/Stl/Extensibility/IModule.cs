using Microsoft.Extensions.DependencyInjection;

namespace Stl.Extensibility
{
    public interface IModule
    {
        IServiceCollection Services { get; set; }
        void ConfigureServices();
    }
}
