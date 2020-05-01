using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Stl.Fusion.Internal;

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
        public object? InvalidatedBy { get; }
        protected bool IsDisposed { get; set; }
        protected bool IsUsed => _isUsed != 0;

        public static ComputeContext New(ComputeOptions options, object? invalidatedBy = null)
        {
            var canUseCache = (options & ComputeOptions.Capture) == 0 & invalidatedBy == null;
            var context = canUseCache 
                ? Cache[options] 
                : new ComputeContext(options, invalidatedBy); 
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

        protected ComputeContext(ComputeOptions options, object? invalidatedBy)
        {
            Options = options;
            InvalidatedBy = invalidatedBy;
        }

        public override string ToString() => $"{GetType().Name}({Options}, {InvalidatedBy ?? "null"})";

        public ComputeContextScope Activate() => new ComputeContextScope(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TryCaptureValue(IComputed? value)
        {
            if (value == null || (Options & ComputeOptions.Capture) == 0)
                return;
            // Debug.WriteLine($"Trying to capture: {value} @ {this}");
            Interlocked.CompareExchange(ref _capturedComputed, value, null);
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
        internal CachedComputeContext(ComputeOptions options)
            : base(options, null) 
        { }
    }
}
