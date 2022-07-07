using System.Reflection;
using Stl.Fusion.Authentication;
using Stl.Fusion.Interception;

namespace Stl.Fusion.Tests;

public class ArgumentHandlerProviderTest : SimpleFusionTestBase
{
    public ArgumentHandlerProviderTest(ITestOutputHelper @out) : base(@out) { }

    protected override void ConfigureCommonServices(ServiceCollection services)
    { }

    [Fact]
    public void BasicTest()
    {
        var services = CreateServiceProvider();
        var ahp = services.GetRequiredService<IArgumentHandlerProvider>();

        var method = GetType().GetMethod(nameof(TestMethod), BindingFlags.Instance | BindingFlags.NonPublic)!;
        var parameters = method.GetParameters();
        ahp.GetArgumentHandler(method, parameters[0]).Should().BeOfType<ArgumentHandler>();
        ahp.GetArgumentHandler(method, parameters[1]).Should().BeOfType<EquatableArgumentHandler<string>>();
        ahp.GetArgumentHandler(method, parameters[2]).Should().BeOfType<EquatableArgumentHandler<bool>>();
        ahp.GetArgumentHandler(method, parameters[3]).Should().BeOfType<HasIdArgumentHandler<Symbol>>();
        ahp.GetArgumentHandler(method, parameters[4]).Should().BeOfType<IgnoreArgumentHandler>();
    }

    private void TestMethod(object a, string b, bool c, Session d, CancellationToken ct) 
    { }
}
