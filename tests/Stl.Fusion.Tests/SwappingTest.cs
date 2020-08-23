using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stl.Fusion.Internal;
using Stl.Fusion.Swapping;
using Stl.Testing;
using Stl.Testing.Internal;
using Stl.Tests;
using Xunit;
using Xunit.Abstractions;
using Xunit.DependencyInjection.Logging;

namespace Stl.Fusion.Tests
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class SwappingTest : TestBase
    {
        public class Service
        {
            public int CallCount { get; set; }

            [ComputeMethod(KeepAliveTime = 0.5)]
            [Swap(1)]
            public virtual async Task<object> SameValueAsync(object x)
            {
                await Task.Yield();
                CallCount++;
                return x;
            }
        }

        public class SwapService : SimpleSwapService
        {
            public int LoadCallCount { get; set; }
            public int RenewCallCount { get; set; }
            public int StoreCallCount { get; set; }

            public SwapService(Options? options = null) : base(options) { }

            protected override ValueTask<Option<string>> LoadAsync(string key, CancellationToken cancellationToken)
            {
                LoadCallCount++;
                return base.LoadAsync(key, cancellationToken);
            }

            protected override ValueTask<bool> RenewAsync(string key, CancellationToken cancellationToken)
            {
                RenewCallCount++;
                return base.RenewAsync(key, cancellationToken);
            }

            protected override ValueTask StoreAsync(string key, string value, CancellationToken cancellationToken)
            {
                StoreCallCount++;
                return base.StoreAsync(key, value, cancellationToken);
            }
        }

        public SwappingTest(ITestOutputHelper @out) : base(@out) { }

        public IServiceProvider CreateProviderFor<TService>()
            where TService : class
        {
            ComputedRegistry.Instance = new ComputedRegistry(new ComputedRegistry.Options() {
                InitialCapacity = 16,
            });
            var services = new ServiceCollection();
            services.AddLogging(logging => {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddDebug();
                logging.AddProvider(
                    new XunitTestOutputLoggerProvider(
                        new TestOutputHelperAccessor(Out)));
            });
            services.AddFusionCore();
            services.AddSingleton<SwapService>();
            services.AddSingleton(c => new SimpleSwapService.Options {
                TimerQuanta = TimeSpan.FromSeconds(0.1),
                ExpirationTime = TimeSpan.FromSeconds(3),
            });
            services.AddSingleton<ISwapService, LoggingSwapServiceWrapper<SwapService>>();
            services.AddSingleton(c => new LoggingSwapServiceWrapper<SwapService>.Options() {
                LogLevel = LogLevel.Information,
            });

            services.AddComputeService<TService>();
            return services.BuildServiceProvider();
        }

        [Fact]
        public async void BasicTest()
        {
            if (TestRunnerInfo.GitHub.IsActionRunning)
                // TODO: Fix intermittent failures on GitHub
                return;

            var services = CreateProviderFor<Service>();
            var swapService = services.GetRequiredService<SwapService>();
            var service = services.GetRequiredService<Service>();
            var clock = Timeouts.Clock;

            service.CallCount = 0;
            var a = "a";
            var v = await service.SameValueAsync(a);
            service.CallCount.Should().Be(1);
            swapService.LoadCallCount.Should().Be(0);
            swapService.StoreCallCount.Should().Be(0);
            swapService.RenewCallCount.Should().Be(0);
            v.Should().BeSameAs(a);

            await DelayAsync(1.3);
            swapService.LoadCallCount.Should().Be(0);
            swapService.RenewCallCount.Should().Be(1);
            swapService.StoreCallCount.Should().Be(1);
            v = await service.SameValueAsync(a);
            service.CallCount.Should().Be(1);
            swapService.LoadCallCount.Should().Be(1);
            swapService.RenewCallCount.Should().Be(1);
            swapService.StoreCallCount.Should().Be(1);
            v.Should().Be(a);
            v.Should().NotBeSameAs(a);

            // We accessed the value, so we need to wait for
            // SwapTime + KeepAliveTime to make sure it's GC-ed
            await DelayAsync(1.9);
            swapService.LoadCallCount.Should().Be(1);
            swapService.RenewCallCount.Should().Be(2);
            swapService.StoreCallCount.Should().Be(1);
            await GCCollectAsync();
            v = await service.SameValueAsync(a);
            service.CallCount.Should().Be(2);
            swapService.LoadCallCount.Should().Be(1);
            swapService.RenewCallCount.Should().Be(2);
            swapService.StoreCallCount.Should().Be(1);
            v.Should().Be(a);
            v.Should().BeSameAs(a);
        }

        private Task DelayAsync(double seconds)
            => Timeouts.Clock.DelayAsync(TimeSpan.FromSeconds(seconds));

        private async Task GCCollectAsync()
        {
            GC.Collect();
            await Task.Delay(10);
            GC.Collect();
            await Task.Delay(10);
            GC.Collect(); // To collect what has finalizers
        }
    }
}
