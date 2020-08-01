using Castle.DynamicProxy;

namespace Stl.Fusion.Internal
{
    public abstract class ProxyGeneratorBase<TOptions>
        where TOptions : ProxyGenerationOptions, new()
    {
        protected TOptions ProxyGeneratorOptions { get; }
        protected ModuleScope ModuleScope { get; }

        public ProxyGeneratorBase(
            TOptions? options = null,
            ModuleScope? moduleScope = null)
        {
            ProxyGeneratorOptions = options ??= new TOptions();
            ModuleScope = moduleScope ??= new ModuleScope();
        }
    }
}
