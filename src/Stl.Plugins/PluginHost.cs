using System;
using System.Linq;
using Stl.Extensibility;
using Stl.Internal;

namespace Stl.Plugins
{
    public interface IPluginHost : IServiceProvider, IDisposable
    {
        bool IsStarted { get; }
        // Must return actual IServiceProvider hosting plugins
        IServiceProvider Plugins { get; }
        void Start();
    }

    public class PluginHost : IPluginHost
    {
        public IServiceProvider Plugins { get; private set; }
        public bool IsStarted { get; protected set; }

        public PluginHost(IServiceProvider plugins) => Plugins = plugins;

        protected virtual void Dispose(bool disposing)
        {
            var plugins = Plugins;
            Plugins = null!;
            if (plugins is IDisposable disposable)
                disposable.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual object GetService(Type serviceType) => Plugins.GetService(serviceType);

        public virtual void Start()
        {
            if (IsStarted)
                throw Errors.AlreadyInvoked(nameof(Start));
            IsStarted = true;
            var plugins = this.GetPlugins<IHasStart>().ToArray();
            var invoker = Invoker.New(plugins, 
                (plugin, _) => plugin.Start(), 
                InvocationOrder.Reverse);
            invoker.Run();
        }
    }
}
