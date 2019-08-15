using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Stl.Plugins.Metadata;

namespace Stl.Plugins.Internal
{
    public interface IPluginInstanceHandle : IDisposable
    {
        object UntypedInstance { get; }
    }

    public interface IPluginInstanceHandle<out TPluginImpl> : IPluginInstanceHandle
        where TPluginImpl : notnull
    {
        TPluginImpl Instance { get; }
    }
    
    public class PluginInstanceHandle<TPluginImpl> : IPluginInstanceHandle<TPluginImpl>
        where TPluginImpl : notnull
    {
        private Lazy<TPluginImpl>? _lazyInstance;
        public TPluginImpl Instance => 
            (_lazyInstance ?? throw new ObjectDisposedException(GetType().Name)).Value;
        // ReSharper disable once HeapView.BoxingAllocation
        public object UntypedInstance => Instance;

        public PluginInstanceHandle(IServiceProvider services)
        {
            var pluginSetInfo = services.GetService<PluginSetInfo>();
            var pluginType = typeof(TPluginImpl);
            if (!pluginSetInfo.Implementations.ContainsKey(pluginType))
                throw Errors.UnknownPluginImplementationType(pluginType.Name);
            _lazyInstance = new Lazy<TPluginImpl>(
                () => {
                    var preferredCtor = pluginType.GetConstructor(
                        new [] {typeof(IServiceProvider)});
                    var instance = preferredCtor != null
                        ? Activator.CreateInstance(pluginType, services)
                        : Activator.CreateInstance(pluginType);
                    return (TPluginImpl) instance!; 
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
