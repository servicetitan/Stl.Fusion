using Stl.Testing.Collections;

namespace Stl.Fusion.Tests;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class KeepAliveTest : TestBase
{
    public class Service
    {
        public ThreadSafe<int> CallCount { get; }

        [ComputeMethod(KeepAliveTime = 0.5)]
        public virtual async Task<double> Sum(double a, double b)
        {
            await Task.Yield();
            CallCount.Value++;
            return a + b;
        }

        [ComputeMethod]
        public virtual async Task<double> Multiply(double a, double b)
        {
            await Task.Yield();
            CallCount.Value++;
            return a * b;
        }
    }

    public KeepAliveTest(ITestOutputHelper @out) : base(@out) { }

    public static IServiceProvider CreateProviderFor<TService>()
        where TService : class
    {
        ComputedRegistry.Instance = new ComputedRegistry(new ComputedRegistry.Options() {
            InitialCapacity = 16,
        });
        var services = new ServiceCollection();
        services.AddFusion().AddComputeService<TService>();
        return services.BuildServiceProvider();
    }

    [SkipOnGitHubFact]
    public async Task TestNoKeepAlive()
    {
        var services = CreateProviderFor<Service>();
        var service = services.GetRequiredService<Service>();

        service.CallCount.Value = 0;
        await service.Multiply(1, 1);
        service.CallCount.Value.Should().Be(1);
        await service.Multiply(1, 1);
        service.CallCount.Value.Should().Be(1);

        await GCCollect();
        await service.Multiply(1, 1);
        service.CallCount.Value.Should().Be(2);
    }

    [SkipOnGitHubFact]
    public async Task TestKeepAlive()
    {
        var services = CreateProviderFor<Service>();
        var service = services.GetRequiredService<Service>();

        service.CallCount.Value = 0;
        await service.Sum(1, 1);
        service.CallCount.Value.Should().Be(1);
        await service.Sum(1, 1);
        service.CallCount.Value.Should().Be(1);

        await Task.Delay(250);
        await GCCollect();
        await service.Sum(1, 1);
        service.CallCount.Value.Should().Be(1);

        await Task.Delay(1000);
        await GCCollect();
        await service.Sum(1, 1);
        service.CallCount.Value.Should().Be(2);
    }

    private async Task GCCollect()
    {
        GC.Collect();
        await Task.Delay(10);
        GC.Collect();
        await Task.Delay(10);
        GC.Collect(); // To collect what has finalizers
    }
}
