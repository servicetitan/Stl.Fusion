using Stl.Fusion.Extensions;
using Stl.Multitenancy;

namespace Stl.Fusion.Tests.Extensions;

public class NestedOperationLoggerTest : FusionTestBase
{
    public NestedOperationLoggerTest(ITestOutputHelper @out) : base(@out)
        => UseTestClock = true;

    [Fact]
    public async Task BasicTest()
    {
        var kvs = Services.GetRequiredService<IKeyValueStore>();
        var tenantId = Tenant.Default.Id;

        var c1 = await Computed.Capture(() => kvs.Get(tenantId, "1"));
        var c2 = await Computed.Capture(() => kvs.Get(tenantId, "2"));
        var c3 = await Computed.Capture(() => kvs.Get(tenantId, "3"));
        c1.Value.Should().BeNull();
        c2.Value.Should().BeNull();
        c3.Value.Should().BeNull();

        var commander = Services.Commander();
        var command = new NestedOperationLoggerTester_SetMany(
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
