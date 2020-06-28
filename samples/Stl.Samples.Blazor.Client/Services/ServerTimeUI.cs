using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion;
using Stl.Fusion.UI;

namespace Stl.Samples.Blazor.Client.Services
{
    public class ServerTimeUI : ILiveUpdater<ServerTimeUI.Model>
    {
        public class Model
        {
            public DateTime? Time { get; set; }
            public string FormattedTime => Time?.ToString("HH:mm:ss.ffff") ?? "n/a";
        }

        protected ITimeClient Client { get; }

        public ServerTimeUI(ITimeClient client) => Client = client;

        public virtual async Task<Model> UpdateAsync(
            IComputed<Model> prevComputed, CancellationToken cancellationToken)
        {
            var time = await Client.GetTimeAsync(cancellationToken);
            return new Model() { Time = time };
        }
    }
}
