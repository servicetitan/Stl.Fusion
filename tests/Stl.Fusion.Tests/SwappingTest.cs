using Stl.Fusion.Swapping;
using Stl.Testing.Collections;

namespace Stl.Fusion.Tests;

public class SwappingTestService : IComputeService
{
    public ThreadSafe<int> CallCount { get; } = 0;

    [ComputeMethod(MinCacheDuration = 0.5)]
    [Swap(1)]
    public virtual async Task<object> SameValue(object x)
    {
        await Task.Yield();
        CallCount.Value++;
        return x;
    }
}

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class SwappingTest : SimpleFusionTestBase
{
    public class SwapService : SimpleSwapService
    {
        public ThreadSafe<int> LoadCallCount { get; } = 0;
        public ThreadSafe<int> TouchCallCount { get; } = 0;
        public ThreadSafe<int> StoreCallCount { get; } = 0;

        public SwapService(Options options, IServiceProvider services)
            : base(options, services) { }

        protected override ValueTask<string?> Load(string key, CancellationToken cancellationToken)
        {
            LoadCallCount.Value++;
            return base.Load(key, cancellationToken);
        }

        protected override ValueTask<bool> Touch(string key, CancellationToken cancellationToken)
        {
            TouchCallCount.Value++;
            return base.Touch(key, cancellationToken);
        }

        protected override ValueTask Store(string key, string value, CancellationToken cancellationToken)
        {
            StoreCallCount.Value++;
            return base.Store(key, value, cancellationToken);
        }
    }

    public SwappingTest(ITestOutputHelper @out) : base(@out) { }

    protected override void ConfigureCommonServices(ServiceCollection services)
    {
        services.AddSingleton<SwapService>();
        services.AddSingleton(c => new SimpleSwapService.Options {
            TimerQuanta = TimeSpan.FromSeconds(0.1),
            ExpirationTime = TimeSpan.FromSeconds(3),
        });
        services.AddSingleton<LoggingSwapServiceWrapper<SwapService>.Options>();
        services.AddSingleton<ISwapService, LoggingSwapServiceWrapper<SwapService>>();
    }

    [SkipOnGitHubFact]
    public async Task BasicTest()
    {
        var services = CreateServiceProviderFor<SwappingTestService>();
        var swapService = services.GetRequiredService<SwapService>();
        var service = services.GetRequiredService<SwappingTestService>();

        service.CallCount.Value = 0;
        var a = "a";
        var v = await service.SameValue(a);
        v.Should().BeSameAs(a);

        service.CallCount.Value.Should().Be(1);
        swapService.LoadCallCount.Value.Should().Be(0);
        swapService.StoreCallCount.Value.Should().Be(0);
        swapService.TouchCallCount.Value.Should().Be(0);

        await Delay(1.4); // 1s swap time

        swapService.LoadCallCount.Value.Should().Be(0);
        swapService.TouchCallCount.Value.Should().Be(1);
        swapService.StoreCallCount.Value.Should().Be(1);

        v = await service.SameValue(a);
        v.Should().Be(a);
        v.Should().NotBeSameAs(a);

        service.CallCount.Value.Should().Be(1);
        swapService.LoadCallCount.Value.Should().Be(1);
        swapService.TouchCallCount.Value.Should().Be(1);
        swapService.StoreCallCount.Value.Should().Be(1);

        // We accessed the value, so we need to wait for
        // SwapDelay + KeepAliveTime to make sure it's
        // available for GC
        await Delay(1.9);
        swapService.LoadCallCount.Value.Should().Be(1);
        swapService.TouchCallCount.Value.Should().Be(2);
        swapService.StoreCallCount.Value.Should().Be(1);

        for (var i = 0; i < 10; i++) {
            GCCollect();
            v = await service.SameValue(a);
            if (service.CallCount.Value != 1)
                break;
            await Delay(0.1);
        }
        v.Should().Be(a);
        v.Should().BeSameAs(a);

        service.CallCount.Value.Should().Be(2);
        swapService.LoadCallCount.Value.Should().Be(1);
        swapService.TouchCallCount.Value.Should().Be(2);
        swapService.StoreCallCount.Value.Should().Be(1);
    }
}
