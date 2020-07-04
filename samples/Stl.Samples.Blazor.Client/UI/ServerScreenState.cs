using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion.UI;
using Stl.Samples.Blazor.Client.Services;
using Stl.Samples.Blazor.Common.Services;

namespace Stl.Samples.Blazor.Client.UI
{
    public class ServerScreenState
    {
        public Screenshot Screenshot { get; set; } = new Screenshot(0, 0, "");

        public class Local
        {
            private int _width = 1280;

            public int Width {
                get => _width;
                set {
                    if (_width == value)
                        return;
                    _width = value;
                    LiveState?.Invalidate();
                }
            }

            public int ActualWidth => Math.Max(8, Math.Min(1920, Width));

            public ILiveState<Local, ServerScreenState>? LiveState { get; set; }
        }
                                    
        public class Updater : ILiveStateUpdater<Local, ServerScreenState>
        {
            protected IScreenshotClient Client { get; }

            public Updater(IScreenshotClient client) => Client = client;

            public virtual async Task<ServerScreenState> UpdateAsync(
                ILiveState<Local, ServerScreenState> liveState, CancellationToken cancellationToken)
            {
                var local = liveState.Local;
                var screenshot = await Client.GetScreenshotAsync(local.ActualWidth, cancellationToken);
                return new ServerScreenState() { Screenshot = screenshot };
            }
        }
    }
}
