using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Serilog.Core;
using Stl.Internal;

namespace Stl.Plugins 
{
    public abstract class Plugin : IHasLog, IDisposable
    {
        public bool IsInitialized => Host != null;
        public string Name { get; protected set; }
        public IPluginHost? Host { get; private set; }
        public ILogger Log { get; private set; } = Logger.None;
        public IEnumerable<Type> Dependencies { get; private set; } = Enumerable.Empty<Type>();
        public IEnumerable<InjectionPoint> InjectionPoints { get; private set; }  = Enumerable.Empty<InjectionPoint>();

        protected Plugin(string? name = null) => Name = name ?? GetType().Name;
        public override string ToString() => Name;

        protected virtual void Dispose(bool disposing)
        {
            Log.Information($"Disposing.");
            var injectionPoints = InjectionPoints;
            InjectionPoints = Enumerable.Empty<InjectionPoint>();
            foreach (var point in injectionPoints)
                point?.Dispose();
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Initialize(IPluginHost host)
        {
            if (IsInitialized)
                throw Errors.AlreadyInitialized();
            Host = host ?? throw new ArgumentNullException(nameof(host));;
            Log = host.Log.ForContext(GetType());
            Log.Information($"Initializing.");
            Dependencies = AcquireDependencies().ToArray();
            var injectionPoints = AcquireInjectionPoints().ToArray();
            foreach (var point in injectionPoints)
                point.Initialize(this);
            InjectionPoints = injectionPoints;
            Log.Debug($"Injection points: {string.Join(", ", InjectionPoints)}.");
        }

        protected virtual ILogger AcquireLogger() => Host?.Log ?? Logger.None;
        protected virtual IEnumerable<Type> AcquireDependencies() => Enumerable.Empty<Type>();
        protected abstract IEnumerable<InjectionPoint> AcquireInjectionPoints();
        
    }
    
    public abstract class Plugin<THost> : Plugin
        where THost : class, IPluginHost
    {
        new THost? Host => (THost?) base.Host;
        public void Initialize(THost host) => base.Initialize(host);
    }
}
