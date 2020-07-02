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
            public int Width { get; set; } = 1280;            
        }
                                    
        public class Updater : ILiveStateUpdater<Local, ServerScreenState>
        {
            protected IScreenshotClient Client { get; }

            public Updater(IScreenshotClient client) => Client = client;

            public virtual async Task<ServerScreenState> UpdateAsync(
                ILiveState<Local, ServerScreenState> liveState, CancellationToken cancellationToken)
            {
                var local = liveState.Local;
                var screenshot = await Client.GetScreenshotAsync(local.Width, cancellationToken);
                return new ServerScreenState() { Screenshot = screenshot };
            }
        }
    }
}
