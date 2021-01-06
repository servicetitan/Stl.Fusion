using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Collections;
using Stl.CommandR.Configuration;
using Stl.Reflection;
using Stl.CommandR.Internal;
using Stl.DependencyInjection;

namespace Stl.CommandR
{
    public abstract class CommandContext : IHasServices
    {
        private static readonly AsyncLocal<CommandContext?> CurrentLocal = new();
        public static CommandContext? Current => CurrentLocal.Value;

        public abstract ICommand UntypedCommand { get; }
        public abstract Task UntypedResultTask { get; }
        public abstract Result<object> UntypedResult { get; set; }
        public NamedValueSet Items { get; protected set; } = null!;
        public CommandContext? OuterContext { get; protected set; }
        public CommandContext OutermostContext { get; protected set; } = null!;
        public IReadOnlyList<CommandHandler> Handlers { get; set; } = ArraySegment<CommandHandler>.Empty;
        public int NextHandlerIndex { get; set; }
        public IServiceProvider Services { get; }

        // Static methods

        internal static CommandContext<TResult> New<TResult>(ICommand command, IServiceProvider services)
            => new(command, services);
        internal static CommandContext New(ICommand command, IServiceProvider services)
        {
            var tContext = typeof(CommandContext<>).MakeGenericType(command.ResultType);
            return (CommandContext) tContext.CreateInstance(command, services);
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

        protected CommandContext(IServiceProvider services)
            => Services = services;

        // Instance methods

        public ClosedDisposable<CommandContext> Activate()
        {
            if (Items != null)
                throw Errors.CommandContextWasActivatedEarlier();
            OuterContext = Current;
            OutermostContext = OuterContext?.OutermostContext ?? this;
            Items = OuterContext?.Items ?? new NamedValueSet();
            CurrentLocal.Value = this;
            return Disposable.NewClosed(this, self => CurrentLocal.Value = self.OuterContext);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CommandContext<TResult> Cast<TResult>()
            => (CommandContext<TResult>) this;

        public abstract Task InvokeNextHandlerAsync(CancellationToken cancellationToken);

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

        public CommandContext(ICommand command, IServiceProvider services)
            : base(services)
        {
            var tResult = typeof(TResult);
            if (command.ResultType != tResult)
                throw Errors.CommandResultTypeMismatch(tResult, command.ResultType);
            Command = (ICommand<TResult>) command;
            ResultTaskSource = TaskSource.New<TResult>(true);
        }

        // Instance methods

        public override async Task InvokeNextHandlerAsync(CancellationToken cancellationToken)
        {
            try {
                if (NextHandlerIndex >= Handlers.Count)
                    throw Errors.NoFinalHandlerFound(UntypedCommand);
                var handler = Handlers[NextHandlerIndex++];
                var resultTask = handler.InvokeAsync(UntypedCommand, this, cancellationToken);
                if (resultTask is Task<TResult> typedResultTask) {
                    var result = await typedResultTask.ConfigureAwait(false);
                    TrySetResult(result);
                }
                else {
                    await resultTask.ConfigureAwait(false);
                    TrySetDefaultResult();
                }
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
