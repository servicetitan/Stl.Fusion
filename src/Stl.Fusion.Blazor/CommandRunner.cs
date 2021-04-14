using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Stl.CommandR;

namespace Stl.Fusion.Blazor
{
    public class CommandRunner
    {
        private Exception? _error;

        public Exception? Error {
            get => _error;
            set {
                if (_error == value)
                    return;
                _error = value;
                Component?.StateHasChangedAsync();
            }
        }

        public ICommander Commander { get; }
        public ComponentBase? Component { get; set; }

        public CommandRunner(ICommander commander)
            => Commander = commander;

        public Task Call(ICommand command, CancellationToken cancellationToken = default)
            => Call(command, false, cancellationToken);

        public async Task Call(
            ICommand command,
            bool throwOnError,
            CancellationToken cancellationToken = default)
        {
            Error = null;
            try {
                await Commander.Call(command, cancellationToken);
            }
            catch (Exception e) {
                Error = e;
                if (throwOnError)
                    throw;
            }
            finally {
                TryApplyUserCausedUpdate();
            }
        }

        public Task<TResult> Call<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
            => Call(command, false, cancellationToken);

        public async Task<TResult> Call<TResult>(
            ICommand<TResult> command,
            bool throwOnError,
            CancellationToken cancellationToken = default)
        {
            Error = null;
            try {
                return await Commander.Call(command, cancellationToken);
            }
            catch (Exception e) {
                Error = e;
                if (throwOnError)
                    throw;
                return default!;
            }
            finally {
                TryApplyUserCausedUpdate();
            }
        }

        private void TryApplyUserCausedUpdate()
        {
            if (Component is StatefulComponentBase { UntypedState: IComputedState computedState })
                computedState.ApplyUserCausedUpdate();
        }
    }
}
