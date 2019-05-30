using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Internal;

namespace Stl.Plugins 
{
    public abstract class Plugin : IHasLogger, IDisposable
    {
        public bool IsInitialized => Host != null;
        public string Name { get; protected set; }
        public IPluginHost? Host { get; private set; }
        public ILogger Logger { get; private set; } = NullLogger.Instance;
        public IEnumerable<Type> Dependencies { get; private set; } = Enumerable.Empty<Type>();
        public IEnumerable<InjectionPoint> InjectionPoints { get; private set; }  = Enumerable.Empty<InjectionPoint>();

        protected Plugin(string? name = null) => Name = name ?? GetType().Name;
        public override string ToString() => Name;

        protected virtual void Dispose(bool disposing)
        {
            Logger.LogInformation($"Disposing.");
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
            Logger = host.LoggerFactory.CreateLogger(GetType());
            Logger.LogInformation($"Initializing.");
            Dependencies = AcquireDependencies().ToArray();
            var injectionPoints = AcquireInjectionPoints().ToArray();
            foreach (var point in injectionPoints)
                point.Initialize(this);
            InjectionPoints = injectionPoints;
            Logger.LogDebug($"Injection points: {string.Join(", ", InjectionPoints)}.");
        }

        protected virtual ILogger AcquireLogger() => Host?.Logger ?? NullLogger.Instance;
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
