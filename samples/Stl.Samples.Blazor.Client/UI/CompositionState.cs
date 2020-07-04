using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion.UI;
using Stl.Samples.Blazor.Client.Services;
using Stl.Samples.Blazor.Common.Services;

namespace Stl.Samples.Blazor.Client.UI
{
    public class CompositionState
    {
        public ComposedValue LocallyComposedValue { get; set; } = new ComposedValue();
        public ComposedValue RemotelyComposedValue { get; set; } = new ComposedValue();

        public class Local
        {
            private string _parameter = "Type something here";

            public string Parameter {
                get => _parameter;
                set {
                    if (_parameter == value)
                        return;
                    _parameter = value;
                    LiveState?.Invalidate();
                }
            }

            public ILiveState<Local, CompositionState>? LiveState { get; set; }
        }

        public class Updater : ILiveStateUpdater<Local, CompositionState>
        {
            protected IComposerService LocalComposer { get; }
            protected IComposerClient RemoteComposer { get; }

            public Updater(IComposerService localComposer, IComposerClient remoteComposer)
            {
                LocalComposer = localComposer;
                RemoteComposer = remoteComposer;
            }

            public virtual async Task<CompositionState> UpdateAsync(
                ILiveState<Local, CompositionState> liveState, CancellationToken cancellationToken)
            {
                var local = liveState.Local;
                var localValue = await LocalComposer.GetComposedValueAsync(local.Parameter, cancellationToken);
                var remoteValue = await RemoteComposer.GetComposedValueAsync(local.Parameter, cancellationToken);
                return new CompositionState() {
                    LocallyComposedValue = localValue,
                    RemotelyComposedValue = remoteValue,
                };
            }
        }
    }
}
