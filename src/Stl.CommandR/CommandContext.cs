using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Collections;
using Stl.Reflection;
using Stl.CommandR.Internal;
using Stl.DependencyInjection;

namespace Stl.CommandR
{
    public abstract class CommandContext : IHasServiceProvider
    {
        private static readonly AsyncLocal<CommandContext?> CurrentLocal = new();
        public static CommandContext? Current => CurrentLocal.Value;

        public abstract ICommand UntypedCommand { get; }
        public abstract Task UntypedResultTask { get; }
        public PropertyBag Globals { get; protected set; }
        public PropertyBag Locals { get; protected set; }
        public CommandContext? Parent { get; protected set; }
        public IReadOnlyList<CommandHandler> Handlers { get; set; } = ArraySegment<CommandHandler>.Empty;
        public int NextHandlerIndex { get; set; }
        public IServiceProvider ServiceProvider { get; }

        // Static methods

        public static CommandContext<TResult> New<TResult>(ICommand command, IServiceProvider serviceProvider)
            => new(command, serviceProvider);
        public static CommandContext New(ICommand command, IServiceProvider serviceProvider)
        {
            var tContext = typeof(CommandContext<>).MakeGenericType(command.ResultType);
            return (CommandContext) tContext.CreateInstance(command, serviceProvider);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CommandContext GetCurrent()
            => Current ?? throw Errors.NoCurrentCommandContext();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CommandContext<TResult> GetCurrent<TResult>()
            => GetCurrent().Cast<TResult>();

        // Constructors

        protected CommandContext(IServiceProvider serviceProvider)
        {
            Globals = null!;
            Locals = new();
            ServiceProvider = serviceProvider;
        }

        // Instance methods

        public ClosedDisposable<CommandContext> Activate()
        {
            Parent = Current;
            Globals = Parent?.Globals ?? new PropertyBag();
            CurrentLocal.Value = this;
            return Disposable.NewClosed(this, self => CurrentLocal.Value = self.Parent);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CommandContext<TResult> Cast<TResult>()
            => (CommandContext<TResult>) this;

        public Task InvokeNextHandlerAsync(CancellationToken cancellationToken)
        {
            if (NextHandlerIndex >= Handlers.Count)
                return Task.CompletedTask;
            var handler = Handlers[NextHandlerIndex++];
            return handler.InvokeAsync(UntypedCommand, this, cancellationToken);
        }

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
        public ICommand<TResult> Command { get; }
        public TaskSource<TResult> ResultTaskSource { get; }
        public Task<TResult> ResultTask => ResultTaskSource.Task;
        public override Task UntypedResultTask => ResultTask;
        public override ICommand UntypedCommand => Command;

        public CommandContext(ICommand command, IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            var tResult = typeof(TResult);
            if (command.ResultType != tResult)
                throw Errors.CommandResultTypeMismatch(tResult, command.ResultType);
            Command = (ICommand<TResult>) command;
            ResultTaskSource = TaskSource.New<TResult>(true);
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
