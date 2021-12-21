using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Tests.Services;
using Stl.RegisterAttributes;

namespace Stl.Fusion.Tests.UIModels;

[RegisterService(typeof(IComputedState<ServerTimeModel1>))]
public class ServerTimeModel1State : ComputedState<ServerTimeModel1>
{
    private IClientTimeService Client
        => Services.GetRequiredService<IClientTimeService>();

    public ServerTimeModel1State(IServiceProvider services)
        : base(new() { InitialValue = new(default) }, services) { }

    protected override async Task<ServerTimeModel1> Compute(CancellationToken cancellationToken)
    {
        var time = await Client.GetTime(cancellationToken).ConfigureAwait(false);
        return new ServerTimeModel1(time);
    }
}
