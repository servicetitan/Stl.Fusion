using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Bridge;
using Stl.Fusion.Tests.Services;
using Stl.Testing;
using Stl.Testing.Collections;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Fusion.Tests
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class ReplicationTest : FusionTestBase, IAsyncLifetime
    {
        public ReplicationTest(ITestOutputHelper @out) : base(@out) { }

        [Fact(Timeout = 120_000)]
        public async Task BasicTest()
        {
            await using var serving = await WebHost.Serve();
            var publisher = WebServices.GetRequiredService<IPublisher>();
            var replicator = ClientServices.GetRequiredService<IReplicator>();
            using var scope = WebServices.CreateScope();
            var sp = scope.ServiceProvider.GetRequiredService<ISimplestProvider>();

            sp.SetValue("");
            var p1 = await publisher.Publish(_ => sp.GetValue());
            p1.Should().NotBeNull();

            var r1 = replicator.GetOrAdd<string>(p1.Ref, true);
            var r1c = await r1.Computed.Update();
            r1c.IsConsistent().Should().BeTrue();
            r1c.Value.Should().Be("");
            r1.Computed.Should().Be(r1c);

            sp.SetValue("1");
            await Task.Delay(100);
            r1c.IsConsistent().Should().BeFalse();
            r1.Computed.Should().Be(r1c);

            r1c = await r1c.Update();
            r1c.Value.Should().Be("1");

            var r1c1 = await r1c.Update();
            r1c1.Should().Be(r1c);
        }

        [Fact(Timeout = 120_000)]
        public async Task TimerTest()
        {
            await using var serving = await WebHost.Serve();
            var publisher = WebServices.GetRequiredService<IPublisher>();
            var replicator = ClientServices.GetRequiredService<IReplicator>();
            var tp = WebServices.GetRequiredService<ITimeService>();

            var pub = await publisher.Publish(_ => tp.GetTime());
            var rep = replicator.GetOrAdd<DateTime>(pub.Ref);

            var count = 0;
            using var state = Services.StateFactory().NewComputed<DateTime>(
                UpdateDelayer.ZeroDelay,
                async (_, ct) => await rep.Computed.Use(ct));
            state.Updated += (s, _) => {
                Out.WriteLine($"{s.Value}");
                count++;
            };

            await TestExt.WhenMet(
                () => count.Should().BeGreaterThan(2),
                TimeSpan.FromSeconds(5));
        }
    }
}
