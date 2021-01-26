using Microsoft.Extensions.DependencyInjection;

namespace Stl.Extensibility
{
    public abstract class ModuleBase : IModule
    {
        public IServiceCollection Services { get; }

        protected ModuleBase(IServiceCollection services)
            => Services = services;

        public abstract void Use();
    }
}
