using System.Diagnostics.CodeAnalysis;

namespace Stl.Plugins.Internal;

public interface IPluginFactory
{
    object? Create(Type pluginType);
}

public class PluginFactory(IServiceProvider services) : IPluginFactory
{
    protected IServiceProvider Services { get; } = services;

#pragma warning disable IL2092
    public virtual object? Create(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type pluginType)
        => Services.Activate(pluginType);
#pragma warning restore IL2092
}
