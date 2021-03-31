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

        public async Task Call<TResult>(ICommand command, CancellationToken cancellationToken = default)
        {
            Error = null;
            try {
                await Commander.Call(command, cancellationToken);
                TryInvalidate();
            }
            catch (Exception e) {
                Error = e;
            }
        }

        public async Task<TResult> Call<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
        {
            Error = null;
            try {
                var result = await Commander.Call(command, cancellationToken);
                TryInvalidate();
                return result;
            }
            catch (Exception e) {
                Error = e;
                return default!;
            }
        }

        private void TryInvalidate()
        {
            if (Component is not StatefulComponentBase statefulComponent)
                return;
            if (statefulComponent.UntypedState is not ILiveState liveState)
                return;
            liveState.Invalidate();
            liveState.UpdateDelayer.CancelDelays();
        }
    }
}
