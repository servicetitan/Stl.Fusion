using System.Diagnostics.CodeAnalysis;
using Stl.Fusion.Internal;

namespace Stl.Fusion;

public class ComputeContext
{
    private static readonly ComputeContext[] ContextCache;
    private static readonly AsyncLocal<ComputeContext?> CurrentLocal = new();
    private volatile IComputed? _captured;

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

    public CallOptions CallOptions { get; private set; }

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
        if (null == Interlocked.CompareExchange(ref _captured, computed, null))
            CallOptions &= ~CallOptions.Capture; 
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IComputed GetCaptured() => _captured ?? throw Errors.NoComputedCaptured();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IComputed<T> GetCaptured<T>() => (IComputed<T>) GetCaptured();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetCaptured([MaybeNullWhen(false)] out IComputed computed)
    {
        computed = _captured;
        return computed != null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetCaptured<T>([MaybeNullWhen(false)] out IComputed<T> computed)
    {
        computed = _captured as IComputed<T>;
        return computed != null;
    }
}

internal class CachedComputeContext : ComputeContext
{
    internal CachedComputeContext(CallOptions callOptions) : base(callOptions) { }
}
