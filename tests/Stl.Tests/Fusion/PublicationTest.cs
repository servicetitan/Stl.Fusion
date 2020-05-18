using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Stl.Fusion;
using Stl.Fusion.Bridge.Messages;
using Stl.Testing;
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
            Publisher.ChannelHub.Attach(cp.Channel1).Should().BeTrue();
            var cReader = cp.Channel2.Reader;

            var sp = Container.Resolve<ISimplestProvider>();
            sp.SetValue("");

            var p1 = await Computed.PublishAsync(Publisher, () => sp.GetValueAsync());
            p1.Should().NotBeNull();

            Publisher.Subscribe(cp.Channel1, p1!, true).Should().BeTrue();
            await Task.Delay(1000);
            var m = await cReader.AssertReadAsync();
            m.Should().BeOfType<SubscribeMessage>();
            
            sp.SetValue("1");
            m = await cReader.AssertReadAsync();
            m.Should().BeOfType<StateChangeMessage<string>>()
                .Which.NewIsConsistent.Should().BeFalse();
            m.PublisherId.Should().Be(Publisher.Id);
            m.PublicationId.Should().Be(p1!.Id);
                
            m = await cReader.AssertReadAsync();
            m.Should().BeOfType<StateChangeMessage<string>>()
                .Which.Output.Value.Should().Be("1");
            
            sp.SetValue("12");
            m = await cReader.AssertReadAsync();
            m.Should().BeOfType<StateChangeMessage<string>>()
                .Which.NewIsConsistent.Should().BeFalse();
            m.Should().BeOfType<StateChangeMessage<string>>()
                .Which.Output.Value.Should().Be("12");

            await Publisher.UnsubscribeAsync(cp.Channel1, p1);
            m = await cReader.AssertReadAsync();
            m.Should().BeOfType<UnsubscribeMessage>();
        }

    }
}
