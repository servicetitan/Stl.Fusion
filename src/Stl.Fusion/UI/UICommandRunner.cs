using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.CommandR;

namespace Stl.Fusion.UI
{
    public record UICommandRunner
    {
        public ICommander Commander { get; init; }
        public IUICommandTracker UICommandTracker { get; init; }
        public bool ThrowOnError { get; init; } = true;

        public UICommandRunner(ICommander commander, IUICommandTracker uiCommandTracker)
        {
            Commander = commander;
            UICommandTracker = uiCommandTracker;
        }

        public Task<TResult> Run<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
            => Run(command, ThrowOnError, cancellationToken);

        public async Task<TResult> Run<TResult>(
            ICommand<TResult> command,
            bool throwOnError,
            CancellationToken cancellationToken = default)
        {
            var completedEvent = await Run((ICommand) command, throwOnError, cancellationToken).ConfigureAwait(false);
            return completedEvent.Result!.Cast<TResult>().Value;
        }

        public Task<UICommandEvent> Run(ICommand command, CancellationToken cancellationToken = default)
            => Run(command, ThrowOnError, cancellationToken);

        public virtual async Task<UICommandEvent> Run(
            ICommand command,
            bool throwOnError,
            CancellationToken cancellationToken = default)
        {
            var commandEvent = CreateCommandEvent(command);
            commandEvent = UICommandTracker.ProcessEvent(commandEvent);

            IResult result;
            try {
                var context = await Commander.Run(command, true, cancellationToken).ConfigureAwait(false);
                await context.UntypedResultTask.ConfigureAwait(false);
                result = Result.FromTypedTask(context.UntypedResultTask);
            }
            catch (Exception e) {
                result = Result.Error(command.GetResultType(), e);
            }

            var completedEvent = commandEvent with { Result = result };
            completedEvent = UICommandTracker.ProcessEvent(completedEvent);

            if (result.HasError && throwOnError)
                ExceptionDispatchInfo.Capture(result.Error!).Throw();
            return completedEvent;
        }

        // Protected methods

        protected virtual UICommandEvent CreateCommandEvent(ICommand command)
            => new(command);
    }
}
