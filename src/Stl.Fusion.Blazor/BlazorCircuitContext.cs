using Microsoft.AspNetCore.Components;
using Stl.Internal;

namespace Stl.Fusion.Blazor;

public class BlazorCircuitContext(IServiceProvider services) : ProcessorBase
{
    private static long _lastId;

    private readonly TaskCompletionSource<Unit> _whenReady = TaskCompletionSourceExt.New<Unit>();
    private volatile int _isPrerendering;
    private ComponentBase? _rootComponent;
    private Dispatcher? _dispatcher;
    private ILogger? _log;

    protected ILogger Log => _log ??= Services.LogFor(GetType());

    public IServiceProvider Services { get; } = services;
    public long Id { get; } = Interlocked.Increment(ref _lastId);
    public Task WhenReady => _whenReady.Task;
    public bool IsPrerendering => _isPrerendering != 0;
    public Dispatcher Dispatcher => _dispatcher ??= RootComponent.GetDispatcher();

    public ComponentBase RootComponent {
        get => _rootComponent ?? throw Errors.NotInitialized(nameof(RootComponent));
        set {
            if (Interlocked.CompareExchange(ref _rootComponent, value, null) != null)
                throw Errors.AlreadyInitialized(nameof(RootComponent));

            _whenReady.TrySetResult(default);
        }
    }

    public ClosedDisposable<(BlazorCircuitContext, int)> Prerendering(bool isPrerendering = true)
    {
        var oldIsPrerendering = Interlocked.Exchange(ref _isPrerendering, isPrerendering ? 1 : 0);
        return new ClosedDisposable<(BlazorCircuitContext Context, int OldIsPrerendering)>(
            (this, oldIsPrerendering),
            state => Interlocked.Exchange(ref state.Context._isPrerendering, state.OldIsPrerendering));
    }
}
