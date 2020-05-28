using System;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Stl.Channels;
using Stl.Fusion;
using Stl.Fusion.Bridge;
using Stl.Tests.Fusion.Services;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Fusion
{
    public class ReplicationTest : FusionTestBase, IAsyncLifetime
    {
        public ReplicationTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task BasicTest()
        {
            await using var serving = await WebSocketServer.ServeAsync();
            var sp = Container.Resolve<ISimplestProvider>();

            sp.SetValue("");
            var (p1, _) = await Publisher.PublishAsync(() => sp.GetValueAsync());
            p1.Should().NotBeNull();

            var r1 = Replicator.GetOrAdd<string>(p1!.Publisher.Id, p1.Id, true);
            var r1c = await r1.Computed.UpdateAsync();
            r1c.IsConsistent.Should().BeTrue();
            r1c.Value.Should().Be("");
            r1.Computed.Should().Be(r1c);

            sp.SetValue("1");
            await Task.Delay(10);
            r1c.IsConsistent.Should().BeFalse();
            r1.Computed.Should().Be(r1c);

            await r1.RequestUpdateAsync();
            r1c = r1.Computed;
            r1c.Value.Should().Be("1");

            await r1.RequestUpdateAsync();
            r1.Computed.Should().Be(r1c);
        }

        [Fact]
        public async Task TimerTest()
        {
            await using var serving = await WebSocketServer.ServeAsync();
            var tp = Container.Resolve<ITimeProvider>();

            var (pub, _) = await Publisher.PublishAsync(() => tp.GetTimeAsync());
            var rep = Replicator.GetOrAdd<DateTime>(pub!.Publisher.Id, pub.Id);
            await rep.RequestUpdateAsync();

            var count = 0;
            using var _ = rep.Computed.AutoUpdate((c, o, _) => {
                Out.WriteLine($"{c.Value}");
                count++;
            });

            await Task.Delay(2000);
            count.Should().BeGreaterThan(4);
        }
    }
}
