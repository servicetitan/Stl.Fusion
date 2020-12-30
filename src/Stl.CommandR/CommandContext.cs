using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Collections;
using Stl.Reflection;
using Stl.CommandR.Internal;

namespace Stl.CommandR
{
    public abstract class CommandContext
    {
        private static readonly AsyncLocal<CommandContext?> CurrentLocal = new();
        public static CommandContext? Current => CurrentLocal.Value;

        public ICommand Command { get; }
        public PropertyBag Globals { get; protected set; }
        public PropertyBag Locals { get; protected set; }
        public CommandContext? Parent { get; protected set; }

        public static CommandContext<TResult> New<TResult>(ICommand command) => new(command);
        public static CommandContext New(ICommand command)
        {
            var tContext = typeof(CommandContext<>).MakeGenericType(command.ResultType);
            return (CommandContext) tContext.CreateInstance(command);
        }

        protected CommandContext(ICommand command)
        {
            Command = command;
            Globals = null!;
            Locals = new();
        }

        public ClosedDisposable<CommandContext> Activate()
        {
            Parent = Current;
            Globals = Parent?.Globals ?? new PropertyBag();
            CurrentLocal.Value = this;
            return Disposable.NewClosed(this, self => CurrentLocal.Value = self.Parent);
        }
    }

    public class CommandContext<TResult> : CommandContext, ICommandContextImpl
    {
        public TaskSource<TResult> ResultTaskSource { get; }
        public Task<TResult> ResultTask => ResultTaskSource.Task;

        public CommandContext(ICommand command) : base(command)
        {
            var tResult = typeof(TResult);
            if (command.ResultType != tResult)
                throw new ArgumentException($"Command result type mismatch: expected {tResult}, got {command.ResultType}");
            ResultTaskSource = TaskSource.New<TResult>(true);
        }

        void ICommandContextImpl.TrySetDefaultResult()
            => ResultTaskSource.TrySetResult(default!);
        void ICommandContextImpl.TrySetException(Exception exception)
            => ResultTaskSource.TrySetException(exception);
        void ICommandContextImpl.TrySetCancelled(CancellationToken cancellationToken)
            => ResultTaskSource.TrySetCanceled(cancellationToken);
    }
}
