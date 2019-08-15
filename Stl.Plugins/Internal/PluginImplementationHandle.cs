using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Stl.Plugins.Metadata;

namespace Stl.Plugins.Internal
{
    public interface IPluginImplementationHandle : IDisposable
    {
        object UntypedInstance { get; }
    }

    public interface IPluginImplementationHandle<out TPlugin> : IPluginImplementationHandle
        where TPlugin : notnull
    {
        TPlugin Instance { get; }
    }
    
    public class PluginImplementationHandle<TPlugin> : IPluginImplementationHandle<TPlugin>
        where TPlugin : notnull
    {
        private Lazy<TPlugin>? _lazyInstance;
        public TPlugin Instance => 
            (_lazyInstance ?? throw new ObjectDisposedException(GetType().Name)).Value;
        // ReSharper disable once HeapView.BoxingAllocation
        public object UntypedInstance => Instance;

        public PluginImplementationHandle(IServiceProvider services)
        {
            var pluginSetInfo = services.GetService<PluginSetInfo>();
            var pluginType = typeof(TPlugin);
            if (!pluginSetInfo.Implementations.ContainsKey(pluginType))
                throw Errors.UnknownPluginImplementationType(pluginType.Name);
            _lazyInstance = new Lazy<TPlugin>(
                () => {
                    var preferredCtor = pluginType.GetConstructor(
                        new [] {typeof(IServiceProvider)});
                    var plugin = preferredCtor != null
                        ? Activator.CreateInstance(pluginType, services)
                        : Activator.CreateInstance(pluginType);
                    return (TPlugin) plugin!; 
                }, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        protected virtual void Dispose(bool disposing)
        {
            var lazyInstance = _lazyInstance;
            _lazyInstance = null;
            var disposable = lazyInstance == null ? null : lazyInstance.Value as IDisposable;
            disposable?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
