using Microsoft.Extensions.Options;

namespace Stl.DependencyInjection.Internal;

public class ConfigureAllNamedOptions<TOptions>: IConfigureNamedOptions<TOptions>
    where TOptions : class
{
    private readonly IServiceProvider _services;
    private readonly Action<IServiceProvider, string?, TOptions> _configure;

    public ConfigureAllNamedOptions(IServiceProvider services, Action<IServiceProvider, string?, TOptions> configure)
    {
        _services = services;
        _configure = configure;
    }

    public void Configure(string name, TOptions options)
    {
        _configure.Invoke(_services, name, options);
    }

    // This won't be called, but is required for the interface
    public void Configure(TOptions options)
        => Configure(Options.DefaultName, options);
}
