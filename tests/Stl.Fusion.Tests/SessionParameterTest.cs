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
    public class SessionParameterTest : SimpleFusionTestBase
    {
        public SessionParameterTest(ITestOutputHelper @out) : base(@out) { }

        protected override void ConfigureCommonServices(ServiceCollection services)
            => services.AddFusion().AddAuthentication();

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
            var sessionFactory = services.GetRequiredService<ISessionFactory>();
            var sessionA = sessionFactory.CreateSession();
            var sessionB = sessionFactory.CreateSession();

            var session = sessionA;
            var aaComputed = await Computed.CaptureAsync(_ => counters.GetAsync("a", session));
            Task.Run(() => WatchAsync(nameof(aaComputed), aaComputed)).Ignore();
            var abComputed = await Computed.CaptureAsync(_ => counters.GetAsync("b", session));
            Task.Run(() => WatchAsync(nameof(abComputed), abComputed)).Ignore();

            session = sessionB;
            var baComputed = await Computed.CaptureAsync(_ => counters.GetAsync("a", session));
            Task.Run(() => WatchAsync(nameof(baComputed), baComputed)).Ignore();

            session = sessionA;
            await counters.IncrementAsync("a", session);
            (await aaComputed.UpdateAsync(false)).Value.Should().Be(1);
            (await abComputed.UpdateAsync(false)).Value.Should().Be(0);
            (await baComputed.UpdateAsync(false)).Value.Should().Be(0);
            await counters.IncrementAsync("b", session);
            (await aaComputed.UpdateAsync(false)).Value.Should().Be(1);
            (await abComputed.UpdateAsync(false)).Value.Should().Be(1);
            (await baComputed.UpdateAsync(false)).Value.Should().Be(0);

            session = sessionB;
            await counters.IncrementAsync("a", session);
            (await aaComputed.UpdateAsync(false)).Value.Should().Be(1);
            (await abComputed.UpdateAsync(false)).Value.Should().Be(1);
            (await baComputed.UpdateAsync(false)).Value.Should().Be(1);
            await counters.IncrementAsync("b", session);
            (await aaComputed.UpdateAsync(false)).Value.Should().Be(1);
            (await abComputed.UpdateAsync(false)).Value.Should().Be(1);
            (await baComputed.UpdateAsync(false)).Value.Should().Be(1);

            stopCts.Cancel();
        }
    }
}
