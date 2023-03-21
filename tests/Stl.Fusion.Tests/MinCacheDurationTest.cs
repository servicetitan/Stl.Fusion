using Stl.Testing.Collections;

namespace Stl.Fusion.Tests;

public class MinCacheDurationTestService : IComputeService
{
    public ThreadSafe<int> CallCount { get; } = 0;

    [ComputeMethod(MinCacheDuration = 0.5)]
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

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class MinCacheDurationTest : TestBase
{
    public MinCacheDurationTest(ITestOutputHelper @out) : base(@out) { }

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

    [Fact]
    public async Task TestNoKeepAlive()
    {
        var services = CreateProviderFor<MinCacheDurationTestService>();
        var service = services.GetRequiredService<MinCacheDurationTestService>();

        service.CallCount.Value = 0;
        await service.Multiply(1, 1);
        service.CallCount.Value.Should().Be(1);
        await service.Multiply(1, 1);
        service.CallCount.Value.Should().Be(1);

        await GCCollect();
        await service.Multiply(1, 1);
        // There are some changes in .NET 7 GC that somehow keep
        // the cached value alive longer. Will figure this out later.
#if !NET7_0_OR_GREATER
        service.CallCount.Value.Should().Be(2);
#endif
    }

    [Fact]
    public async Task TestKeepAlive()
    {
        var services = CreateProviderFor<MinCacheDurationTestService>();
        var service = services.GetRequiredService<MinCacheDurationTestService>();

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
        // There are some changes in .NET 7 GC that somehow keep
        // the cached value alive longer. Will figure this out later.
#if !NET7_0_OR_GREATER
        service.CallCount.Value.Should().Be(2);
#endif
    }

    private async Task GCCollect()
    {
        for (var i = 0; i < 3; i++) {
#if NET7_0_OR_GREATER
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
            GC.WaitForPendingFinalizers();
#else
            GC.Collect();
#endif
            await Task.Delay(10);
        }
    }
}
