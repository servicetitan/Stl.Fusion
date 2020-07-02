using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion.UI;
using Stl.Samples.Blazor.Client.Services;
using Stl.Samples.Blazor.Common.Services;

namespace Stl.Samples.Blazor.Client.UI
{
    public class CompositionUI
    {
        public ILive<CompositionUI>? Live { get; set; }
        public ComposedValue LocallyComposedValue { get; set; } = new ComposedValue();
        public ComposedValue RemotelyComposedValue { get; set; } = new ComposedValue();

        private string _parameter = "";
        public string Parameter {
            get => _parameter;
            set {
                if (_parameter == value)
                    return;
                _parameter = value;
                if (Live != null) {
                    Live.Invalidate();
                    Live.UpdateDelayer.CancelDelays();
                }
            }
        }

        public class Updater : ILiveUpdater<CompositionUI>
        {
            protected IComposerService LocalComposer { get; }
            protected IComposerClient RemoteComposer { get; }

            public Updater(IComposerService localComposer, IComposerClient remoteComposer)
            {
                LocalComposer = localComposer;
                RemoteComposer = remoteComposer;
            }

            public virtual async Task<CompositionUI> UpdateAsync(
                ILive<CompositionUI> live, CancellationToken cancellationToken)
            {
                var prevModel = live.Value;
                var parameter = prevModel.Parameter;
                var localValue = await LocalComposer.GetComposedValueAsync(parameter, cancellationToken);
                var remoteValue = await RemoteComposer.GetComposedValueAsync(parameter, cancellationToken);
                return new CompositionUI() {
                    Live = live,
                    LocallyComposedValue = localValue,
                    RemotelyComposedValue = remoteValue,
                    Parameter = parameter,
                };
            }
        }
    }
}
