using Stl.Interception;

namespace Stl.Tests.Interception;

public interface ITestTypedFactory : IRequiresFullProxy
{
    TestFactoryClass CreateTestClass(int int32);
}

public class TestFactoryClass(IServiceProvider services, int int32)
{
    public IServiceProvider Services { get; } = services;
    public int Int32 { get; } = int32;
}

public class TypedFactoryTest
{
    [Fact]
    public void BasicTest()
    {
        var services = new ServiceCollection()
            .UseTypedFactories()
            .AddTypedFactory<ITestTypedFactory>()
            .BuildServiceProvider();

        var factory = services.GetRequiredService<ITestTypedFactory>();
        var x = factory.CreateTestClass(7);
        x.Int32.Should().Be(7);
        var y = factory.CreateTestClass(8);
        y.Int32.Should().Be(8);
        y.Services.Should().BeSameAs(x.Services);
    }

    [Fact]
    public void ScopedTest()
    {
        var services = new ServiceCollection()
            .UseTypedFactories()
            .AddTypedFactory<ITestTypedFactory>(ServiceLifetime.Scoped)
            .BuildServiceProvider();

        var factory = services.GetRequiredService<ITestTypedFactory>();
        var x = factory.CreateTestClass(7);
        x.Int32.Should().Be(7);

        using var c1 = services.CreateScope();
        var x1 = c1.ServiceProvider.GetRequiredService<ITestTypedFactory>().CreateTestClass(1);
        x1.Int32.Should().Be(1);
        x1.Services.Should().BeSameAs(c1);

        using var c2 = services.CreateScope();
        var x2 = c2.ServiceProvider.GetRequiredService<ITestTypedFactory>().CreateTestClass(2);
        x2.Int32.Should().Be(2);
        x2.Services.Should().BeSameAs(c2);

        x1.Should().NotBeSameAs(x);
        x2.Should().NotBeSameAs(x);
        x2.Should().NotBeSameAs(x1);
    }
}
