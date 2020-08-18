using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.Testing;
using Stl.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Fusion.Tests
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class KeepAliveTest : TestBase
    {
        public class Calculator
        {
            public int CallCount { get; set; }

            [ComputeMethod(KeepAliveTime = 0.5)]
            public virtual async Task<double> SumAsync(double a, double b)
            {
                await Task.Yield();
                CallCount++;
                return a + b;
            }

            [ComputeMethod]
            public virtual async Task<double> MulAsync(double a, double b)
            {
                await Task.Yield();
                CallCount++;
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
            services.AddFusionCore();
            services.AddComputeService<TService>();
            return services.BuildServiceProvider();
        }

        [Fact]
        public async void TestNoKeepAlive()
        {
            if (TestRunnerInfo.GitHub.IsActionRunning)
                // TODO: Fix intermittent failures on GitHub
                return;

            var services = CreateProviderFor<Calculator>();
            var r = ComputedRegistry.Instance;
            var c = services.GetRequiredService<Calculator>();

            c.CallCount = 0;
            await c.MulAsync(1, 1);
            c.CallCount.Should().Be(1);
            await c.MulAsync(1, 1);
            c.CallCount.Should().Be(1);

            await GCCollectAsync();
            await c.MulAsync(1, 1);
            c.CallCount.Should().Be(2);
        }

        [Fact]
        public async void TestKeepAlive()
        {
            if (TestRunnerInfo.GitHub.IsActionRunning)
                // TODO: Fix intermittent failures on GitHub
                return;

            var services = CreateProviderFor<Calculator>();
            var r = ComputedRegistry.Instance;
            var c = services.GetRequiredService<Calculator>();

            c.CallCount = 0;
            await c.SumAsync(1, 1);
            c.CallCount.Should().Be(1);
            await c.SumAsync(1, 1);
            c.CallCount.Should().Be(1);

            await Task.Delay(250);
            await GCCollectAsync();
            await c.SumAsync(1, 1);
            c.CallCount.Should().Be(1);

            await Task.Delay(1000);
            await GCCollectAsync();
            await c.SumAsync(1, 1);
            c.CallCount.Should().Be(2);
        }

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
