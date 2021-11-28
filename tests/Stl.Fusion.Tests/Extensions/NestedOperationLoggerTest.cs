using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Extensions;

namespace Stl.Fusion.Tests.Extensions;

public class NestedOperationLoggerTest : FusionTestBase
{
    public NestedOperationLoggerTest(ITestOutputHelper @out)
        : base(@out, new FusionTestOptions() { UseTestClock = true })
    { }

    [Fact]
    public async Task BasicTest()
    {
        var kvs = Services.GetRequiredService<IKeyValueStore>();
        var c1 = await Computed.Capture(_ => kvs.Get("1"));
        var c2 = await Computed.Capture(_ => kvs.Get("2"));
        var c3 = await Computed.Capture(_ => kvs.Get("3"));
        c1.Value.Should().BeNull();
        c2.Value.Should().BeNull();
        c3.Value.Should().BeNull();

        var commander = Services.Commander();
        var command = new NestedOperationLoggerTester.SetManyCommand(
            new[] {"1", "2", "3"}, "v");
        await commander.Call(command);

        c1.IsInvalidated().Should().BeTrue();
        c2.IsInvalidated().Should().BeTrue();
        c3.IsInvalidated().Should().BeTrue();
        c1 = await c1.Update();
        c2 = await c2.Update();
        c3 = await c3.Update();
        c1.Value.Should().Be("v3");
        c2.Value.Should().Be("v2");
        c3.Value.Should().Be("v1");
    }
}
