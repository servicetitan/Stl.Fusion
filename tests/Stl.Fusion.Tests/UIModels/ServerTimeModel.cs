using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Tests.Services;
using Stl.RegisterAttributes;

namespace Stl.Fusion.Tests.UIModels
{
    public class ServerTimeModel1
    {
        public DateTime Time { get; }

        public ServerTimeModel1() { }
        public ServerTimeModel1(DateTime time) => Time = time;
    }

    public class ServerTimeModel2 : ServerTimeModel1
    {
        public ServerTimeModel2() { }
        public ServerTimeModel2(DateTime time) : base(time) { }
    }

    [RegisterService(typeof(IComputedState<ServerTimeModel1>))]
    public class ServerTimeModel1State : ComputedState<ServerTimeModel1>
    {
        private IClientTimeService Client
            => Services.GetRequiredService<IClientTimeService>();

        public ServerTimeModel1State(IServiceProvider services)
            : base(services) { }

        protected override async Task<ServerTimeModel1> Compute(CancellationToken cancellationToken)
        {
            var time = await Client.GetTime(cancellationToken).ConfigureAwait(false);
            return new ServerTimeModel1(time);
        }
    }
}
