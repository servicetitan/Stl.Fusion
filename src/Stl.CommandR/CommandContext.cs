using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using Stl.CommandR.Internal;
using Stl.DependencyInjection;

namespace Stl.CommandR;

public abstract class CommandContext : ICommandContext, IHasServices, IDisposable
{
    protected static readonly AsyncLocal<CommandContext?> CurrentLocal = new();
    public static CommandContext? Current => CurrentLocal.Value;

    protected internal IServiceScope ServiceScope { get; set; } = null!;
    public ICommander Commander { get; }
    public abstract ICommand UntypedCommand { get; }
    public abstract Task UntypedResultTask { get; }
    public abstract Result<object> UntypedResult { get; }
    public abstract bool IsCompleted { get; }

    public CommandContext? OuterContext { get; protected set; }
    public CommandContext OutermostContext { get; protected set; } = null!;
    public bool IsOutermost => OutermostContext == this;
    public CommandExecutionState ExecutionState { get; set; }
    public IServiceProvider Services => ServiceScope.ServiceProvider;
    public OptionSet Items { get; protected set; } = null!;

    // Static methods

    public static CommandContext New(
        ICommander commander, ICommand command, bool isolate = false)
    {
        var previousContext = isolate ? null : Current;
        var tCommandResult = command.GetResultType();
        var tContext = typeof(CommandContext<>).MakeGenericType(tCommandResult);
        return (CommandContext) tContext.CreateInstance(commander, command, previousContext);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CommandContext GetCurrent()
        => Current ?? throw Errors.NoCurrentCommandContext();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CommandContext<TResult> GetCurrent<TResult>()
        => GetCurrent().Cast<TResult>();

    public static ClosedDisposable<CommandContext> Suppress()
    {
        var oldCurrent = Current;
        CurrentLocal.Value = null;
        return Disposable.NewClosed(oldCurrent!, oldCurrent1 => CurrentLocal.Value = oldCurrent1);
    }

    public ClosedDisposable<CommandContext> Activate()
    {
        var oldCurrent = Current;
        CurrentLocal.Value = this;
        return Disposable.NewClosed(oldCurrent!, oldCurrent1 => CurrentLocal.Value = oldCurrent1);
    }

    // Constructors

    protected CommandContext(ICommander commander)
        => Commander = commander;

    // Disposable

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;

        if (IsOutermost)
            ServiceScope.Dispose();
    }

    // Instance methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CommandContext<TResult> Cast<TResult>()
        => (CommandContext<TResult>) this;

    public abstract Task InvokeRemainingHandlers(CancellationToken cancellationToken = default);

    public abstract void ResetResult();
    public abstract void SetResult(object value);
    public abstract void SetResult(Exception exception);

    public abstract bool TryComplete(CancellationToken candidateToken);
}

public sealed class CommandContext<TResult> : CommandContext
{
    private Result<TResult> _result;
    public ICommand<TResult> Command { get; }
    public Task<TResult> ResultTask; // Set at the very end of the pipeline (via Complete)

    // Result may change while the pipeline runs
    public Result<TResult> Result {
        get => _result;
        set {
            if (IsCompleted)
                return;
            _result = value;
        }
    }

    public override bool IsCompleted => ResultTask.IsCompleted;

    public override ICommand UntypedCommand => Command;
    public override Task UntypedResultTask => ResultTask;
    public override Result<object> UntypedResult => Result.Cast<object>();

    public CommandContext(ICommander commander, ICommand command, CommandContext? previousContext)
        : base(commander)
    {
        var tResult = typeof(TResult);
        var tCommandResult = command.GetResultType();
        if (tCommandResult != tResult)
            throw Errors.CommandResultTypeMismatch(tResult, tCommandResult);
        Command = (ICommand<TResult>) command;
        ResultTask = TaskSource.New<TResult>(true).Task;
        if (previousContext?.Commander != commander) {
            OuterContext = null;
            OutermostContext = this;
            ServiceScope = Commander.Services.CreateScope();
            Items = new OptionSet();
        }
        else {
            OuterContext = previousContext;
            OutermostContext = previousContext!.OutermostContext;
            ServiceScope = OutermostContext.ServiceScope;
            Items = OutermostContext.Items;
        }
    }

    public override async Task InvokeRemainingHandlers(CancellationToken cancellationToken = default)
    {
        try {
            if (ExecutionState.IsFinal)
                throw Errors.NoFinalHandlerFound(UntypedCommand.GetType());
            var handler = ExecutionState.NextHandler;
            ExecutionState = ExecutionState.NextExecutionState;
            var handlerTask = handler.Invoke(UntypedCommand, this, cancellationToken);
            if (handlerTask is Task<TResult> typedHandlerTask) {
                Result = await typedHandlerTask.ConfigureAwait(false);
            }
            else {
                await handlerTask.ConfigureAwait(false);
            }
        }
        catch (Exception ex) {
            SetResult(ex);
            throw;
        }
        // We want to ensure we re-throw any exception even if
        // it wasn't explicitly thrown (i.e. set via SetResult)
        if (!Result.IsValue(out var v, out var e))
            ExceptionDispatchInfo.Capture(e).Throw();
    }

    public override void ResetResult()
        => Result = default;

    public override void SetResult(Exception exception)
        => Result = new Result<TResult>(default!, exception);
    public override void SetResult(object value)
        => Result = new Result<TResult>((TResult) value, null);
    public void SetResult(TResult result)
        => Result = new Result<TResult>(result, null);

    public override bool TryComplete(CancellationToken candidateToken)
        => TaskSource.For(ResultTask).TrySetFromResult(Result, candidateToken);
}
