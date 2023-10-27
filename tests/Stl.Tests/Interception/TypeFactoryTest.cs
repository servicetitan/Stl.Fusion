using Stl.Interception;

namespace Stl.Tests.Interception;

public class TypeFactoryTest
{
    [Fact]
    public void TestMethodBasedFactory()
    {
        ServiceCollection services = new();
        services.UseTypeFactories();
        services.AddSingletonTypeFactory<ITestFactory>();
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<ITestFactory>();
        var instance = factory.Create(7);
        instance.Should().BeOfType(typeof(TestFactoryClass));
        instance.Arg1.Should().Be(7);
        instance.Services.Should().NotBeNull();
    }
}

public class TestFactoryClass(int Arg1, IServiceProvider Services)
{
    public int Arg1 { get; } = Arg1;

    public IServiceProvider Services { get; } = Services;
}

public interface ITestFactory : IRequiresFullProxy
{
    TestFactoryClass Create(int ctorArg0);
}
