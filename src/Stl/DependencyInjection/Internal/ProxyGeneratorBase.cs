using Castle.DynamicProxy;

namespace Stl.DependencyInjection.Internal
{
    public abstract class ProxyGeneratorBase<TOptions>
        where TOptions : ProxyGenerationOptions, IOptions, new()
    {
        protected TOptions ProxyGeneratorOptions { get; }
        protected ModuleScope ModuleScope { get; }

        public ProxyGeneratorBase(
            TOptions? options = null,
            ModuleScope? moduleScope = null)
        {
            options = options.OrDefault();
            moduleScope ??= new ModuleScope();
            ProxyGeneratorOptions = options;
            ModuleScope = moduleScope;
        }
    }
}
