using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion.Tests.Services;
using Stl.Fusion.UI;

namespace Stl.Fusion.Tests.UIModels
{
    public class ServerTimeModel1
    {
        public DateTime? Time { get; }

        public ServerTimeModel1() { }
        public ServerTimeModel1(DateTime time) => Time = time;

        [LiveStateUpdater]
        public class Updater : ILiveStateUpdater<ServerTimeModel1>
        {
            private IClientTimeService Time { get; }

            public Updater(IClientTimeService time) => Time = time;

            public async Task<ServerTimeModel1> UpdateAsync(ILiveState<ServerTimeModel1> liveState, CancellationToken cancellationToken)
            {
                var time = await Time.GetTimeAsync(cancellationToken).ConfigureAwait(false);
                return new ServerTimeModel1(time);
            }
        }
    }

    // We need its second version to run the test w/ IComputed too
    public class ServerTimeModel2 : ServerTimeModel1
    {
        public ServerTimeModel2() { }
        public ServerTimeModel2(DateTime time) : base(time) { }
    }
}
