using Stl.Fusion.Tests.Services;

namespace Stl.Fusion.Tests.UIModels;

public class ServerTimeModel1State : ComputedState<ServerTimeModel1>
{
    private ITimeService TimeService => Services.GetRequiredService<ITimeService>();

    public ServerTimeModel1State(IServiceProvider services)
        : base(new() { InitialValue = new(default) }, services) { }

    protected override async Task<ServerTimeModel1> Compute(CancellationToken cancellationToken)
    {
        var time = await TimeService.GetTime(cancellationToken).ConfigureAwait(false);
        return new ServerTimeModel1(time);
    }
}
