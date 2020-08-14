using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.OS;
using Stl.Testing;
using Stl.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Fusion.Tests
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class PruneTest : TestBase
    {
        public class Calculator
        {
            public Action<double, double>? OnSumAsync;

            [ComputeMethod]
            public virtual async Task<double> SumAsync(double a, double b)
            {
                OnSumAsync?.Invoke(a, b);
                await Task.Delay(100).ConfigureAwait(false);
                return a + b;
            }
        }

        public PruneTest(ITestOutputHelper @out) : base(@out) { }

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
        public async void Test()
        {
            if (TestRunnerInfo.GitHub.IsActionRunning)
                // TODO: Fix intermittent failures on GitHub
                return;

            var services = CreateProviderFor<Calculator>();
            var r = ComputedRegistry.Instance;
            var c = services.GetRequiredService<Calculator>();

            await c.SumAsync(1, 1);

            await Task.Delay(2000);
            var tasks = new List<Task>();
            for (var i = 0; i < 20_000 * HardwareInfo.ProcessorCountPo2; i++)
                tasks.Add(c.SumAsync(2, i));
            await Task.WhenAll(tasks);
            GC.Collect();
            await Task.Delay(1000);

            var failed = true;
            c.OnSumAsync = (a, b) => failed = false;
            await c.SumAsync(1, 1);
            failed.Should().BeFalse();
        }
    }
}
