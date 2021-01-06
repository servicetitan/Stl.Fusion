using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Tests.Services;

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

    [State]
    public class ServerTimeModel1State : LiveState<ServerTimeModel1>
    {
        private IClientTimeService Client
            => Services.GetRequiredService<IClientTimeService>();

        public ServerTimeModel1State(Options options, IServiceProvider services, object? argument = null)
            : base(options, services, argument) { }

        protected override async Task<ServerTimeModel1> ComputeValueAsync(CancellationToken cancellationToken)
        {
            var time = await Client.GetTimeAsync(cancellationToken).ConfigureAwait(false);
            return new ServerTimeModel1(time);
        }
    }
}
