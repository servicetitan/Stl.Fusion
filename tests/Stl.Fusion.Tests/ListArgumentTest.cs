using Stl.Fusion.Tests.Services;

namespace Stl.Fusion.Tests;

public class ListArgumentTest : SimpleFusionTestBase
{
    public ListArgumentTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public async Task BasicTest()
    {
        var services = CreateServicesWithComputeService<MathService>();
        var math = services.GetRequiredService<MathService>();

        var c1 = await Computed.Capture(() => math.Sum(null));
        c1.Value.Should().Be(0);
        var c2 = await Computed.Capture(() => math.Sum(null));
        c2.Should().BeSameAs(c1);

        for (var i = 0; i < 10; i++) {
            var values = Enumerable.Range(0, i).ToArray();
            c1 = await Computed.Capture(() => math.Sum(values));
            c1.Value.Should().Be(values.Sum());

            values = values.ToArray(); // Copy array
            c2 = await Computed.Capture(() => math.Sum(values));
            c2.Value.Should().Be(c1.Value);
#if NETFRAMEWORK
            c2.Should().NotBeSameAs(c1);
#else
            if (values.Length == 0)
                c2.Should().BeSameAs(c1);
            else
                c2.Should().NotBeSameAs(c1);
#endif
        }
    }
}
