using Castle.DynamicProxy;

namespace Stl.Interception.Interceptors;

public abstract class ProxyGeneratorBase<TOptions>
    where TOptions : ProxyGenerationOptions, new()
{
    protected TOptions ProxyGeneratorOptions { get; }
    protected ModuleScope ModuleScope { get; }

    protected ProxyGeneratorBase(
        TOptions options,
        ModuleScope? moduleScope = null)
    {
        moduleScope ??= new ModuleScope();
        ProxyGeneratorOptions = options;
        ModuleScope = moduleScope;
    }
}
