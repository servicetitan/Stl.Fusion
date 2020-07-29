using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Bridge;
using Stl.Fusion.Tests.Services;
using Stl.Tests;
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
            await using var serving = await WebSocketHost.ServeAsync();
            var sp = Services.GetRequiredService<ISimplestProvider>();

            sp.SetValue("");
            var p1 = await Publisher.PublishAsync(_ => sp.GetValueAsync());
            p1.Should().NotBeNull();

            var r1 = Replicator.GetOrAdd<string>(p1!.Publisher.Id, p1.Id, true);
            var r1c = await r1.Computed.UpdateAsync(false);
            r1c.IsConsistent.Should().BeTrue();
            r1c.Value.Should().Be("");
            r1.Computed.Should().Be(r1c);

            sp.SetValue("1");
            await Task.Delay(200);
            r1c.IsConsistent.Should().BeFalse();
            r1.Computed.Should().Be(r1c);

            await r1.RequestUpdateAsync();
            r1c = r1.Computed;
            r1c.Value.Should().Be("1");

            await r1.RequestUpdateAsync();
            r1.Computed.Should().Be(r1c);
        }

        [Fact(Timeout = 120_000)]
        public async Task TimerTest()
        {
            await using var serving = await WebSocketHost.ServeAsync();
            var tp = Services.GetRequiredService<ITimeService>();

            var pub = await Publisher.PublishAsync(_ => tp.GetTimeAsync());
            var rep = Replicator.GetOrAdd<DateTime>(pub!.Publisher.Id, pub.Id);
            await rep.RequestUpdateAsync();

            var count = 0;
            using var _ = rep.Computed.AutoUpdate((c, o, _) => {
                Out.WriteLine($"{c.Value}");
                count++;
            });

            await Task.Delay(2000);
            count.Should().BeGreaterThan(2);
        }
    }
}
