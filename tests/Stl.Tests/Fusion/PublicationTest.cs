using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Stl.Channels;
using Stl.Fusion;
using Stl.Tests.Fusion.Services;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Fusion
{
    public class PublicationTest : FusionTestBase, IAsyncLifetime
    {
        public PublicationTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task BasicTest()
        {
            var cp = CreateChannelPair("c1");
            var _ = cp.TestChannel.Reader.ConsumeSilentAsync();
            ChannelHub.Attach(cp.ConsumerChannel).Should().BeTrue();

            var sp = Container.Resolve<ISimplestProvider>();
            sp.SetValue("");

            var p1 = await Computed.PublishAsync(Publisher, () => sp.GetValueAsync());
            p1.Should().NotBeNull();
            Publisher.Subscribe(cp.ConsumerChannel, p1, true).Should().BeTrue();
            
            await Task.Delay(100);
            sp.SetValue("1");
            await Task.Delay(200);
            sp.SetValue("12");
            await Task.Delay(200);
            await Publisher.UnsubscribeAsync(cp.ConsumerChannel, p1);
            await Task.Delay(500);
        }
    }
}
