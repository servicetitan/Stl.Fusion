using Microsoft.Extensions.DependencyInjection;

namespace Stl.Extensibility
{
    public abstract class ModuleBase : IModule
    {
        public IServiceCollection Services { get; set; } = null!;
        public abstract void ConfigureServices();
    }
}
