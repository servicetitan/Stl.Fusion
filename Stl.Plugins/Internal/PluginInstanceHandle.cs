using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

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

        public PluginInstanceHandle(
            IPluginContainerConfiguration configuration, 
            IPluginFactory factory)
        {
            var implementations = configuration.Implementations;
            var pluginImplType = typeof(TPluginImpl);
            if (!implementations.Plugins.ContainsKey(pluginImplType))
                throw Errors.UnknownPluginImplementationType(pluginImplType.Name);
            _lazyInstance = new Lazy<TPluginImpl>(
                () => (TPluginImpl) factory.Create(pluginImplType)!, 
                LazyThreadSafetyMode.ExecutionAndPublication);
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
