using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Stl.Plugins.Internal;
using Stl.Plugins.Metadata;
using Stl.Reflection;

namespace Stl.Plugins.Services
{
    public interface IPluginInstanceHandle : IDisposable
    {
        object Instance { get; }
    }

    public interface IPluginInstanceHandle<out TPluginImpl> : IPluginInstanceHandle
        where TPluginImpl : notnull
    {
        new TPluginImpl Instance { get; }
    }
    
    public class PluginInstanceHandle<TPluginImpl> : IPluginInstanceHandle<TPluginImpl>
        where TPluginImpl : notnull
    {
        private Lazy<TPluginImpl>? _lazyInstance;
        public TPluginImpl Instance => 
            (_lazyInstance ?? throw new ObjectDisposedException(GetType().Name)).Value;
        // ReSharper disable once HeapView.BoxingAllocation
        object IPluginInstanceHandle.Instance => Instance;

        public PluginInstanceHandle(PluginSetInfo plugins, 
            IPluginFactory pluginFactory, IEnumerable<IPluginFilter> pluginFilters)
        {
            var pluginType = typeof(TPluginImpl);
            var pluginInfo = plugins.InfoByType.GetValueOrDefault(pluginType); 
            if (pluginInfo == null)
                throw Errors.UnknownPluginImplementationType(pluginType);
            if (pluginFilters.Any(f => !f.IsEnabled(pluginInfo)))
                throw Errors.PluginDisabled(pluginType);
            _lazyInstance = new Lazy<TPluginImpl>(
                () => (TPluginImpl) pluginFactory.Create(pluginType)!);
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
