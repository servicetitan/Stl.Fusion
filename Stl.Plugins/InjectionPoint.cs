using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Internal;

namespace Stl.Plugins 
{
    public abstract class InjectionPoint : IHasLogger, IDisposable
    {
        public bool IsInitialized => Plugin != null;
        public Plugin? Plugin { get; private set; }
        public ILogger Logger { get; private set; } = NullLogger.Instance;

        public override string ToString() => $"{GetType().Name} @ {Plugin}";

        protected virtual void Dispose(bool disposing) {}
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Initialize(Plugin plugin)
        {
            if (IsInitialized)
                throw Errors.AlreadyInitialized();
            Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            Logger = plugin.Logger; // Intentional
        }
    }
}
