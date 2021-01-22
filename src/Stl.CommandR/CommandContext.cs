using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Stl.Async;
using Stl.Collections;
using Stl.Reflection;
using Stl.CommandR.Internal;
using Stl.DependencyInjection;

namespace Stl.CommandR
{
    public abstract class CommandContext : ICommandContext, IHasServices, IDisposable
    {
        protected static readonly AsyncLocal<CommandContext?> CurrentLocal = new();
        public static CommandContext? Current => CurrentLocal.Value;

        private bool _isDisposed;
        protected CommandContext? PreviousContext { get; }
        protected internal IServiceScope ServiceScope { get; set; } = null!;

        public ICommander Commander { get; }
        public abstract ICommand UntypedCommand { get; }
        public abstract Task UntypedResultTask { get; }
        public abstract Result<object> UntypedResult { get; set; }
        public CommandContext? OuterContext { get; protected set; }
        public CommandContext OutermostContext { get; protected set; } = null!;
        public CommandExecutionState ExecutionState { get; set; }
        public IServiceProvider Services => ServiceScope.ServiceProvider;
        public OptionSet Items { get; protected set; } = null!;

        // Static methods

        internal static CommandContext<TResult> New<TResult>(
            ICommander commander, ICommand command)
            => new(commander, command);

        internal static CommandContext New(
            ICommander commander, ICommand command)
        {
            var tContext = typeof(CommandContext<>).MakeGenericType(command.ResultType);
            return (CommandContext) tContext.CreateInstance(commander, command);
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

        // Constructors

        protected CommandContext(ICommander commander)
        {
            Commander = commander;
            PreviousContext = Current;
        }

        // Disposable

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;
            DisposeInternal();
        }

        protected virtual void DisposeInternal()
        {
            try {
                if (PreviousContext == null)
                    ServiceScope.Dispose();
            }
            finally {
                CurrentLocal.Value = PreviousContext;
            }
        }

        // Instance methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CommandContext<TResult> Cast<TResult>()
            => (CommandContext<TResult>) this;

        public abstract Task InvokeRemainingHandlersAsync(CancellationToken cancellationToken = default);

        // SetXxx & TrySetXxx

        public abstract void SetDefaultResult();
        public abstract void SetException(Exception exception);
        public abstract void SetCancelled();

        public abstract void TrySetDefaultResult();
        public abstract void TrySetException(Exception exception);
        public abstract void TrySetCancelled(CancellationToken cancellationToken);
    }

    public class CommandContext<TResult> : CommandContext
    {
        protected TaskSource<TResult> ResultTaskSource { get; }

        public ICommand<TResult> Command { get; }
        public Task<TResult> ResultTask => ResultTaskSource.Task;
        public Result<TResult> Result {
            get => Stl.Result.FromTask(ResultTask);
            set {
                if (value.IsValue(out var v, out var e))
                    SetResult(v);
                else
                    SetException(e);
            }
        }

        public override Task UntypedResultTask => ResultTask;
        public override ICommand UntypedCommand => Command;
        public override Result<object> UntypedResult {
            get => Result.Cast<object>();
            set => Result = value.Cast<TResult>();
        }

        public CommandContext(ICommander commander, ICommand command)
            : base(commander)
        {
            var tResult = typeof(TResult);
            if (command.ResultType != tResult)
                throw Errors.CommandResultTypeMismatch(tResult, command.ResultType);
            Command = (ICommand<TResult>) command;
            ResultTaskSource = TaskSource.New<TResult>(true);
            if (PreviousContext?.Commander != commander) {
                OuterContext = null;
                OutermostContext = this;
                ServiceScope = Commander.Services.CreateScope();
                Items = new OptionSet();
            }
            else {
                OuterContext = PreviousContext;
                OutermostContext = PreviousContext!.OutermostContext;
                ServiceScope = OutermostContext.ServiceScope;
                Items = OutermostContext.Items;
            }
            CurrentLocal.Value = this;
        }

        public override async Task InvokeRemainingHandlersAsync(CancellationToken cancellationToken)
        {
            try {
                if (ExecutionState.IsFinal)
                    throw Errors.NoFinalHandlerFound(UntypedCommand.GetType());
                var handler = ExecutionState.NextHandler;
                ExecutionState = ExecutionState.NextExecutionState;
                var handlerTask = handler.InvokeAsync(UntypedCommand, this, cancellationToken);
                if (handlerTask is Task<TResult> typedHandlerTask) {
                    var result = await typedHandlerTask.ConfigureAwait(false);
                    TrySetResult(result);
                }
                else {
                    await handlerTask.ConfigureAwait(false);
                    TrySetDefaultResult();
                }
                // We want to ensure we re-throw any exception even if
                // it wasn't explicitly thrown (i.e. set via TrySetException)
                if (UntypedResultTask.IsCompleted && !UntypedResultTask.IsCompletedSuccessfully)
                    await UntypedResultTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                TrySetCancelled(
                    cancellationToken.IsCancellationRequested ? cancellationToken : default);
                throw;
            }
            catch (Exception e) {
                TrySetException(e);
                throw;
            }
        }

        // SetXxx & TrySetXxx

        public virtual void SetResult(TResult result)
            => ResultTaskSource.SetResult(result);
        public virtual void TrySetResult(TResult result)
            => ResultTaskSource.TrySetResult(result);

        public override void SetDefaultResult()
            => ResultTaskSource.SetResult(default!);
        public override void SetException(Exception exception)
            => ResultTaskSource.SetException(exception);
        public override void SetCancelled()
            => ResultTaskSource.SetCanceled();

        public override void TrySetDefaultResult()
            => ResultTaskSource.TrySetResult(default!);
        public override void TrySetException(Exception exception)
            => ResultTaskSource.TrySetException(exception);
        public override void TrySetCancelled(CancellationToken cancellationToken)
            => ResultTaskSource.TrySetCanceled(cancellationToken);
    }
}
