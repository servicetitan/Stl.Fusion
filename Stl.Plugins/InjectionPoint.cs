using System;
using Serilog;
using Serilog.Core;
using Stl.Internal;

namespace Stl.Plugins 
{
    public abstract class InjectionPoint : IHasLog, IDisposable
    {
        public bool IsInitialized => Plugin != null;
        public Plugin? Plugin { get; private set; }
        public ILogger Log { get; private set; } = Logger.None;

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
            Log = plugin.Log; // Intentional
        }
    }
}
