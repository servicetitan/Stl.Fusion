using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stl.Fusion.Internal;
using Stl.Fusion.Swapping;
using Stl.Testing;
using Stl.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Fusion.Tests
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class SwappingTest : SimpleFusionTestBase
    {
        public class Service
        {
            public int CallCount { get; set; }

            [ComputeMethod(KeepAliveTime = 0.5)]
            [Swap(1)]
            public virtual async Task<object> SameValue(object x)
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

            protected override ValueTask<string?> Load(string key, CancellationToken cancellationToken)
            {
                LoadCallCount++;
                return base.Load(key, cancellationToken);
            }

            protected override ValueTask<bool> Renew(string key, CancellationToken cancellationToken)
            {
                RenewCallCount++;
                return base.Renew(key, cancellationToken);
            }

            protected override ValueTask Store(string key, string value, CancellationToken cancellationToken)
            {
                StoreCallCount++;
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
            services.AddSingleton<ISwapService, LoggingSwapServiceWrapper<SwapService>>();
        }

        [Fact]
        public async void BasicTest()
        {
            if (TestRunnerInfo.IsBuildAgent())
                // TODO: Fix intermittent failures on GitHub
                return;

            var services = CreateServiceProviderFor<Service>();
            var swapService = services.GetRequiredService<SwapService>();
            var service = services.GetRequiredService<Service>();
            var clock = Timeouts.Clock;

            service.CallCount = 0;
            var a = "a";
            var v = await service.SameValue(a);
            service.CallCount.Should().Be(1);
            swapService.LoadCallCount.Should().Be(0);
            swapService.StoreCallCount.Should().Be(0);
            swapService.RenewCallCount.Should().Be(0);
            v.Should().BeSameAs(a);

            await Delay(1.4);
            swapService.LoadCallCount.Should().Be(0);
            swapService.RenewCallCount.Should().Be(1);
            swapService.StoreCallCount.Should().Be(1);
            v = await service.SameValue(a);
            service.CallCount.Should().Be(1);
            swapService.LoadCallCount.Should().Be(1);
            swapService.RenewCallCount.Should().Be(1);
            swapService.StoreCallCount.Should().Be(1);
            v.Should().Be(a);
            v.Should().NotBeSameAs(a);

            // We accessed the value, so we need to wait for
            // SwapTime + KeepAliveTime to make sure it's
            // available for GC
            await Delay(1.9);
            swapService.LoadCallCount.Should().Be(1);
            swapService.RenewCallCount.Should().Be(2);
            swapService.StoreCallCount.Should().Be(1);

            for (var i = 0; i < 10; i++) {
                GCCollect();
                v = await service.SameValue(a);
                if (service.CallCount != 1)
                    break;
                await Delay(0.1);
            }
            service.CallCount.Should().Be(2);
            swapService.LoadCallCount.Should().Be(1);
            swapService.RenewCallCount.Should().Be(2);
            swapService.StoreCallCount.Should().Be(1);
            v.Should().Be(a);
            v.Should().BeSameAs(a);
        }
    }
}
