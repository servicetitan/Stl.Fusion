using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion;
using Stl.Fusion.UI;
using Stl.Samples.Blazor.Client.Services;
using Stl.Samples.Blazor.Common.Services;

namespace Stl.Samples.Blazor.Client.UI
{
    public class ServerScreenUI
    {
        public Screenshot Screenshot { get; set; } = new Screenshot(0, 0, "");
        public int Width { get; set; } = 1280;                            

        // Updater

        public class Updater : ILiveUpdater<ServerScreenUI>
        {
            protected IScreenshotClient Client { get; }

            public Updater(IScreenshotClient client) => Client = client;

            public virtual async Task<ServerScreenUI> UpdateAsync(
                IComputed<ServerScreenUI> prevComputed, CancellationToken cancellationToken)
            {
                var prevModel = prevComputed.UnsafeValue ?? new ServerScreenUI();
                var screenshot = await Client.GetScreenshotAsync(prevModel.Width, cancellationToken);
                return new ServerScreenUI() {
                    Screenshot = screenshot,
                    Width = prevModel.Width,
                };
            }
        }
    }
}
