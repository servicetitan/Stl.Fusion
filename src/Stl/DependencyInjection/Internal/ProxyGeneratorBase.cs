using Castle.DynamicProxy;

namespace Stl.DependencyInjection.Internal
{
    public abstract class ProxyGeneratorBase<TOptions>
        where TOptions : ProxyGenerationOptions, new()
    {
        protected TOptions ProxyGeneratorOptions { get; }
        protected ModuleScope ModuleScope { get; }

        public ProxyGeneratorBase(
            TOptions options,
            ModuleScope? moduleScope = null)
        {
            moduleScope ??= new ModuleScope();
            ProxyGeneratorOptions = options;
            ModuleScope = moduleScope;
        }
    }
}
