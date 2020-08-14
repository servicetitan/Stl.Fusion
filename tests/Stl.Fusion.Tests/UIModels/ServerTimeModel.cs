using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion.Tests.Services;
using Stl.Fusion.UI;

namespace Stl.Fusion.Tests.UIModels
{
    public class ServerTimeModel
    {
        public DateTime Time { get; }

        public ServerTimeModel() { }
        public ServerTimeModel(DateTime time) => Time = time;

        [LiveStateUpdater]
        public class Updater : ILiveStateUpdater<ServerTimeModel>
        {
            private IClientTimeService Client { get; }

            public Updater(IClientTimeService time) => Client = time;

            public async Task<ServerTimeModel> UpdateAsync(ILiveState<ServerTimeModel> liveState, CancellationToken cancellationToken)
            {
                var time = await Client.GetTimeAsync(cancellationToken).ConfigureAwait(false);
                return new ServerTimeModel(time);
            }
        }
    }
}
