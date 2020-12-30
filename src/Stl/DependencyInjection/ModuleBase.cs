using Microsoft.Extensions.DependencyInjection;

namespace Stl.DependencyInjection
{
    public abstract class ModuleBase : IModule
    {
        public IServiceCollection Services { get; set; } = null!;
        public abstract void ConfigureServices();
    }
}
