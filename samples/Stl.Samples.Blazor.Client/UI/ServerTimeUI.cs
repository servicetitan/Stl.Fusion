using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion;
using Stl.Fusion.UI;
using Stl.Samples.Blazor.Client.Services;

namespace Stl.Samples.Blazor.Client.UI
{
    public class ServerTimeUI
    {
        public DateTime? Time { get; set; }
        public string FormattedTime => Time?.ToString("HH:mm:ss.ffff") ?? "n/a";

        public class Updater : ILiveUpdater<ServerTimeUI>
        {
            protected ITimeClient Client { get; }

            public Updater(ITimeClient client) => Client = client;

            public virtual async Task<ServerTimeUI> UpdateAsync(
                IComputed<ServerTimeUI> prevComputed, CancellationToken cancellationToken)
            {
                var time = await Client.GetTimeAsync(cancellationToken);
                return new ServerTimeUI() { Time = time };
            }
        }
    }
}
