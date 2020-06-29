using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Stl.Fusion
{
    public class ComputeContext
    {
        internal static readonly AsyncLocal<ComputeContext?> CurrentLocal = new AsyncLocal<ComputeContext?>();
        private static readonly Dictionary<ComputeOptions, ComputeContext> Cache;
        private volatile IComputed? _capturedComputed;
        private volatile int _isUsed;
        
        public static readonly ComputeContext Default;

        public ComputeOptions Options { get; }
        protected bool IsDisposed { get; set; }
        protected bool IsUsed => _isUsed != 0;

        public static ComputeContext New(ComputeOptions options)
        {
            var canUseCache = (options & ComputeOptions.Capture) == 0;
            var context = canUseCache 
                ? Cache[options] 
                : new ComputeContext(options); 
            return context;
        }

        static ComputeContext()
        {
            var allActions = ComputeOptions.TryGetCached |  ComputeOptions.Invalidate;
            var cache = new Dictionary<ComputeOptions, ComputeContext>();
            for (var i = 0; i <= (int) allActions; i++) {
                var action = (ComputeOptions) i;
                cache[action] = new CachedComputeContext(action);
            }
            Cache = cache;
            Default = New(default);
        }

        protected ComputeContext(ComputeOptions options)
        {
            Options = options;
        }

        public override string ToString() => $"{GetType().Name}({Options})";

        public ComputeContextScope Activate() => new ComputeContextScope(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TryCaptureValue(IComputed? value)
        {
            if (value == null || (Options & ComputeOptions.Capture) == 0)
                return;
            // We capture the the last value
            Interlocked.Exchange(ref _capturedComputed, value);
            // Interlocked.CompareExchange(ref _capturedComputed, value, null);
        }

        public IComputed? GetCapturedComputed() => _capturedComputed;
        public IComputed<T>? GetCapturedComputed<T>() => (IComputed<T>?) _capturedComputed;

        internal bool TrySetIsUsed() 
            => 0 == Interlocked.CompareExchange(ref _isUsed, 1, 0);
        internal void ResetIsUsed() 
            => Interlocked.Exchange(ref _isUsed, 0);
    }

    internal class CachedComputeContext : ComputeContext
    {
        internal CachedComputeContext(ComputeOptions options) : base(options) { }
    }
}
