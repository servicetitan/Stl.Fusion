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
    public class PublisherTest : FusionTestBase, IAsyncLifetime
    {
        public PublisherTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task CommunicationTest()
        {
            var cp = CreateChannelPair("c1");
            Publisher.ChannelHub.Attach(cp.Channel1).Should().BeTrue();
            var cReader = cp.Channel2.Reader;

            var sp = Container.Resolve<ISimplestProvider>();
            sp.SetValue("");

            var p1 = await Computed.PublishAsync(Publisher, () => sp.GetValueAsync());
            p1.Should().NotBeNull();

            (await Publisher.SubscribeAsync(cp.Channel1, p1!, true)).Should().BeTrue();
            var m = await cReader.AssertReadAsync();
            m.Should().BeOfType<PublicationStateChangedMessage<string>>()
                .Which.Output.Value.Should().Be("");
            await cReader.AssertCannotReadAsync();
            
            sp.SetValue("1");
            m = await cReader.AssertReadAsync();
            m.Should().BeOfType<PublicationStateChangedMessage<string>>()
                .Which.NewIsConsistent.Should().BeFalse();
            m.PublisherId.Should().Be(Publisher.Id);
            m.PublicationId.Should().Be(p1!.Id);
            await cReader.AssertCannotReadAsync();
            
            sp.SetValue("12");
            // No auto-update after invalidation
            await cReader.AssertCannotReadAsync();

            (await Publisher.SubscribeAsync(cp.Channel1, p1!, true)).Should().BeTrue();
            m = await cReader.AssertReadAsync();
            m.Should().BeOfType<PublicationStateChangedMessage<string>>()
                .Which.NewIsConsistent.Should().BeTrue();
            m.Should().BeOfType<PublicationStateChangedMessage<string>>()
                .Which.Output.Value.Should().Be("12");
            await cReader.AssertCannotReadAsync();

            await p1.DisposeAsync();
            m = await cReader.AssertReadAsync();
            m.Should().BeOfType<PublicationDisposedMessage>();
        }
    }
}
