namespace Stl.Fusion.UI;

public class UICommander : IHasServices
{
    private static readonly ConcurrentDictionary<Type, Func<UICommander, ICommand, CancellationToken, UIAction>>
        CachedActionFactories = new();

    public IServiceProvider Services { get; }
    public ICommander Commander { get; }
    public UIActionTracker UIActionTracker { get; }
    public IMomentClock Clock { get; }

    public UICommander(IServiceProvider services)
    {
        Services = services;
        Commander = services.Commander();
        UIActionTracker = services.GetRequiredService<UIActionTracker>();
        Clock = UIActionTracker.Clock;
    }

    public async Task<TResult> Call<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        var result = await Run(command, cancellationToken).ConfigureAwait(false);
        return result.Value;
    }

    public async Task<object?> Call(ICommand command, CancellationToken cancellationToken = default)
    {
        var result = await Run(command, cancellationToken).ConfigureAwait(false);
        return result.UntypedValue;
    }

    public Task<UIActionResult<TResult>> Run<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        var action = Start(command, cancellationToken);
        return action.ResultTask;
    }

    public async Task<IUIActionResult> Run(ICommand command, CancellationToken cancellationToken = default)
    {
        var action = Start(command, cancellationToken);
        await action.WhenCompleted().ConfigureAwait(false);
        return action.UntypedResult!;
    }

    public UIAction<TResult> Start<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        var action = CreateUIAction<TResult>(command, cancellationToken);
        UIActionTracker.Register(action);
        return action;
    }

    public UIAction Start(ICommand command, CancellationToken cancellationToken = default)
    {
        var action = CreateUIAction(this, command, cancellationToken);
        UIActionTracker.Register(action);
        return action;
    }

    // Protected methods

    protected virtual UIAction<TResult> CreateUIAction<TResult>(
        ICommand command,
        CancellationToken cancellationToken)
    {
        var typedCommand = (ICommand<TResult>) command;
        var resultTask = Commander.Call(typedCommand, isOutermost: true, cancellationToken);
        var action = new UIAction<TResult>(typedCommand, Clock, resultTask, cancellationToken);
        return action;
    }

    // Private methods

    private static UIAction CreateUIAction(UICommander commander, ICommand command, CancellationToken cancellationToken)
        => CachedActionFactories.GetOrAdd(
            command.GetResultType(),
            static (tCommand, commander1) => (Func<UICommander, ICommand, CancellationToken, UIAction>)commander1
                .GetType()
                .GetMethod(nameof(CreateUIAction), BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(tCommand)
                .CreateDelegate(typeof(Func<UICommander, ICommand, CancellationToken, UIAction>)),
            commander
            ).Invoke(commander, command, cancellationToken);
}
