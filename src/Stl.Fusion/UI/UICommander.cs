using System.Linq.Expressions;

namespace Stl.Fusion.UI;

public class UICommander : IHasServices
{
    private static readonly ConcurrentDictionary<Type, Func<UICommander, ICommand, CancellationToken, UIAction>>
        CachedActionFactories = new();

    public IServiceProvider Services { get; }
    public ICommander Commander { get; }
    public UIActionTracker UIActionTracker { get; }
    public MomentClockSet Clocks { get; }

    public UICommander(IServiceProvider services)
    {
        Services = services;
        Commander = services.Commander();
        UIActionTracker = services.GetRequiredService<UIActionTracker>();
        Clocks = UIActionTracker.Clocks;
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
        var action = new UIAction<TResult>(typedCommand, Clocks.UIClock, resultTask, cancellationToken);
        return action;
    }

    // Private methods

    private static UIAction CreateUIAction(UICommander commander, ICommand command, CancellationToken cancellationToken)
    {
        var factory = CachedActionFactories.GetOrAdd(command.GetResultType(), 
            static (tCommand, commander1) => {
                var mCreateUIAction = commander1
                    .GetType()
                    .GetMethod(nameof(CreateUIAction), BindingFlags.Instance | BindingFlags.NonPublic)!
                    .MakeGenericMethod(tCommand);
                
                var pCommander = Expression.Parameter(typeof(UICommander), "commander");
                var pCommand = Expression.Parameter(typeof(ICommand), "command");
                var pCancellationToken = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
                var eBody = Expression.Call(pCommander, mCreateUIAction, pCommand, pCancellationToken);
                var func = (Func<UICommander, ICommand, CancellationToken, UIAction>) Expression
                    .Lambda(eBody, pCommander, pCommand, pCancellationToken)
                    .Compile();
                return func;
            }, commander);
        return factory.Invoke(commander, command, cancellationToken);
    }
}
