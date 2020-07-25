using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Stl.Fusion
{
    public class ComputeContext
    {
        internal static readonly AsyncLocal<ComputeContext?> CurrentLocal = new AsyncLocal<ComputeContext?>();
        private static readonly Dictionary<CallOptions, ComputeContext> ContextCache;
        private volatile IComputed? _capturedComputed;
        private volatile int _isUsed;

        public static readonly ComputeContext Default;

        public CallOptions CallOptions { get; }
        protected bool IsDisposed { get; set; }
        protected bool IsUsed => _isUsed != 0;

        public static ComputeContext New(CallOptions options)
        {
            var canUseCache = (options & CallOptions.Capture) == 0;
            var context = canUseCache
                ? ContextCache[options]
                : new ComputeContext(options);
            return context;
        }

        static ComputeContext()
        {
            var allCallOptions = CallOptions.TryGetCached |  CallOptions.Invalidate;
            var cache = new Dictionary<CallOptions, ComputeContext>();
            for (var i = 0; i <= (int) allCallOptions; i++) {
                var action = (CallOptions) i;
                cache[action] = new CachedComputeContext(action);
            }
            ContextCache = cache;
            Default = New(default);
        }

        protected ComputeContext(CallOptions callOptions)
            => CallOptions = callOptions;

        public override string ToString() => $"{GetType().Name}({CallOptions})";

        public ComputeContextScope Activate() => new ComputeContextScope(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TryCaptureValue(IComputed? value)
        {
            if (value == null || (CallOptions & CallOptions.Capture) == 0)
                return;
            // We capture the last value
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
        internal CachedComputeContext(CallOptions callOptions) : base(callOptions) { }
    }
}
