using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Stl.Tests.DependencyInjection;

public class ServiceCollectionExtTest : TestBase
{
    public ServiceCollectionExtTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public void RemoveAllTest()
    {
        var d0 = ServiceDescriptor.Singleton(new object());
        var d1 = ServiceDescriptor.Singleton(new object());
        var d2 = ServiceDescriptor.Singleton(new object());
        var d3 = ServiceDescriptor.Singleton(new object());
        var services = new ServiceCollection { d1, d2, d3 };
        services.RemoveAll(x => x == d0);
        services.Count.Should().Be(3);

        services.RemoveAll(x => x == d1);
        services.Count.Should().Be(2);
        services[0].Should().Be(d2);
        services[1].Should().Be(d3);

        services.RemoveAll(x => x == d3);
        services.Count.Should().Be(1);
        services[0].Should().Be(d2);

        services = new ServiceCollection { d1, d2, d3 };
        services.RemoveAll(_ => true);
        services.Count.Should().Be(0);
    }
}
