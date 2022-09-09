using Stl.Fusion.Tests.Services;
using Stl.Testing.Collections;

namespace Stl.Fusion.Tests;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class GetExistingTest : SimpleFusionTestBase
{
    public GetExistingTest(ITestOutputHelper @out) : base(@out) { }

    protected override void ConfigureCommonServices(ServiceCollection services)
        => services.AddFusion().AddAuthentication();

    [Fact]
    public async Task BasicTest()
    {
        var services = CreateServiceProviderFor<CounterService>();
        var counters = services.GetRequiredService<CounterService>();

        var c = Computed.GetExisting(() => counters.Get("a"));
        c.Should().BeNull();

        c = await Computed.Capture(() => counters.Get("a"));
        c.Value.Should().Be(0);
        var c1 = Computed.GetExisting(() => counters.Get("a"));
        c1.Should().BeSameAs(c);

        await counters.Increment("a");
        c.IsConsistent().Should().BeFalse();
        c1 = Computed.GetExisting(() => counters.Get("a"));
        c1.Should().BeNull();
    }

    [Fact]
    public async Task LongWaitTest()
    {
        var services = CreateServiceProviderFor<CounterService>();
        var counters = services.GetRequiredService<CounterService>();

        var key = "a wait";
        var getTask = counters.Get(key);
        await Delay(0.1);

        var c = Computed.GetExisting(() => counters.Get(key));
        c!.ConsistencyState.Should().Be(ConsistencyState.Computing);

        using (Computed.Invalidate())
            _ = counters.Get(key);

        var c1 = Computed.GetExisting(() => counters.Get(key));
        c1.Should().BeSameAs(c);
        c1!.ConsistencyState.Should().Be(ConsistencyState.Computing);

        await getTask;
        await Delay(0.1);
        c.ConsistencyState.Should().Be(ConsistencyState.Invalidated);

        var c2 = Computed.GetExisting(() => counters.Get(key));
        c2.Should().BeNull();
    }
}
