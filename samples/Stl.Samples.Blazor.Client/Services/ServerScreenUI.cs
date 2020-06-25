using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion;
using Stl.Fusion.Client;
using Stl.Samples.Blazor.Common.Services;

namespace Stl.Samples.Blazor.Client.Services
{
    public class ServerScreenUI : ILiveUpdater<ServerScreenUI.Model>
    {
        public class Model
        {
            public Screenshot Screenshot { get; set; } = new Screenshot(0, 0, "");
            public int Width { get; set; } = 1280;                            
        }

        protected IScreenshotClient Client { get; }

        public ServerScreenUI(IScreenshotClient client) => Client = client;

        public virtual async Task<Model> UpdateAsync(
            IComputed<Model> prevComputed, CancellationToken cancellationToken)
        {
            var prevModel = prevComputed.UnsafeValue ?? new Model();
            var screenshot = await Client.GetScreenshotAsync(prevModel.Width, cancellationToken);
            return new Model() {
                Screenshot = screenshot,
                Width = prevModel.Width,
            };
        }
    }
}
