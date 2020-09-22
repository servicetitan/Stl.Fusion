using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Authentication;
using Stl.Fusion.Tests.Services;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Fusion.Tests
{
    public class AuthContextParameterTest : SimpleFusionTestBase
    {
        public AuthContextParameterTest(ITestOutputHelper @out) : base(@out) { }

        protected override void ConfigureCommonServices(ServiceCollection services) { }

        [Fact]
        public async Task BasicTest()
        {
            using var stopCts = new CancellationTokenSource();
            var cancellationToken = stopCts.Token;

            async Task WatchAsync<T>(string name, IComputed<T> computed)
            {
                for (;;) {
                    Out.WriteLine($"{name}: {computed.Value}, {computed}");
                    await computed.WhenInvalidatedAsync(cancellationToken);
                    Out.WriteLine($"{name}: {computed.Value}, {computed}");
                    computed = await computed.UpdateAsync(false, cancellationToken);
                }
            }

            var services = CreateServiceProviderFor<PerUserCounterService>();
            var counters = services.GetRequiredService<PerUserCounterService>();
            var sessionA = new Session("a");
            var sessionB = new Session("b");

            using var _1 = sessionA.Activate();
            var aaComputed = await Computed.CaptureAsync(_ => counters.GetAsync("a"));
            Task.Run(() => WatchAsync(nameof(aaComputed), aaComputed)).Ignore();
            var abComputed = await Computed.CaptureAsync(_ => counters.GetAsync("b"));
            Task.Run(() => WatchAsync(nameof(abComputed), abComputed)).Ignore();

            using var _2 = sessionB.Activate();
            var baComputed = await Computed.CaptureAsync(_ => counters.GetAsync("a"));
            Task.Run(() => WatchAsync(nameof(baComputed), baComputed)).Ignore();

            using var _3 = sessionA.Activate();
            await counters.IncrementAsync("a");
            (await aaComputed.UpdateAsync(false)).Value.Should().Be(1);
            (await abComputed.UpdateAsync(false)).Value.Should().Be(0);
            (await baComputed.UpdateAsync(false)).Value.Should().Be(0);
            await counters.IncrementAsync("b");
            (await aaComputed.UpdateAsync(false)).Value.Should().Be(1);
            (await abComputed.UpdateAsync(false)).Value.Should().Be(1);
            (await baComputed.UpdateAsync(false)).Value.Should().Be(0);

            using var _4 = sessionB.Activate();
            await counters.IncrementAsync("a");
            (await aaComputed.UpdateAsync(false)).Value.Should().Be(1);
            (await abComputed.UpdateAsync(false)).Value.Should().Be(1);
            (await baComputed.UpdateAsync(false)).Value.Should().Be(1);
            await counters.IncrementAsync("b");
            (await aaComputed.UpdateAsync(false)).Value.Should().Be(1);
            (await abComputed.UpdateAsync(false)).Value.Should().Be(1);
            (await baComputed.UpdateAsync(false)).Value.Should().Be(1);

            stopCts.Cancel();
        }
    }
}
