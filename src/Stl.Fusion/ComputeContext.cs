using System.Diagnostics.CodeAnalysis;
using Stl.Fusion.Internal;

namespace Stl.Fusion;

public class ComputeContext
{
    private static readonly ComputeContext[] ContextCache;
    private static readonly AsyncLocal<ComputeContext?> CurrentLocal = new();
    internal volatile IComputed? CapturedComputed;
    private volatile int _isUsed;

    internal static readonly ComputeContext Invalidate;
    public static readonly ComputeContext Default;
    public static ComputeContext Current {
        get => CurrentLocal.Value ?? Default;
        internal set {
            if (value == Default)
                value = null!;
            CurrentLocal.Value = value;
        }
    }

    public CallOptions CallOptions { get; }
    protected bool IsDisposed { get; set; }
    protected bool IsUsed => _isUsed != 0;

    internal static ComputeContext New(CallOptions options)
    {
        var canUseCache = (options & CallOptions.Capture) == 0;
        var context = canUseCache
            ? ContextCache[(int) options]
            : new ComputeContext(options);
        return context;
    }

    static ComputeContext()
    {
        var allCallOptions = CallOptions.GetExisting |  CallOptions.Invalidate;
        var cache = new ComputeContext[1 + (int) allCallOptions];
        for (var i = 0; i <= (int) allCallOptions; i++) {
            var action = (CallOptions) i;
            cache[i] = new CachedComputeContext(action);
        }
        ContextCache = cache;
        Default = New(default);
        Invalidate = New(CallOptions.Invalidate);
    }

    protected ComputeContext(CallOptions callOptions)
        => CallOptions = callOptions;

    public override string ToString() => $"{GetType().Name}({CallOptions})";

    public ComputeContextScope Activate() => new (this);
    public static ComputeContextScope Suppress() => new(Default);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void TryCapture(IComputed? computed)
    {
        if (computed == null || (CallOptions & CallOptions.Capture) == 0)
            return;
        Interlocked.CompareExchange(ref CapturedComputed, computed, null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IComputed GetCaptured() => CapturedComputed ?? throw Errors.NoComputedCaptured();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IComputed<T> GetCaptured<T>() => (IComputed<T>) GetCaptured();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetCaptured([MaybeNullWhen(false)] out IComputed computed)
    {
        computed = CapturedComputed;
        return computed != null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetCaptured<T>([MaybeNullWhen(false)] out IComputed<T> computed)
    {
        computed = CapturedComputed as IComputed<T>;
        return computed != null;
    }

    internal bool Acquire()
        => 0 == Interlocked.CompareExchange(ref _isUsed, 1, 0);
    internal void Release()
        => Interlocked.Exchange(ref _isUsed, 0);
}

internal class CachedComputeContext : ComputeContext
{
    internal CachedComputeContext(CallOptions callOptions) : base(callOptions) { }
}
