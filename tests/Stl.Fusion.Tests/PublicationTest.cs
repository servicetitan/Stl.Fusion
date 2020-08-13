using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Messages;
using Stl.Fusion.Tests.Services;
using Stl.Testing;
using Stl.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Fusion.Tests
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class PublisherTest : FusionTestBase, IAsyncLifetime
    {
        public PublisherTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task CommunicationTest()
        {
            var cp = CreateChannelPair("c1");
            Publisher.ChannelHub.Attach(cp.Channel1).Should().BeTrue();
            var cReader = cp.Channel2.Reader;

            var sp = Services.GetRequiredService<ISimplestProvider>();
            sp.SetValue("");

            var p1 = await Publisher.PublishAsync(_ => sp.GetValueAsync());
            p1.Should().NotBeNull();

            (await Publisher.SubscribeAsync(cp.Channel1, p1!, true)).Should().BeTrue();
            var m = await cReader.AssertReadAsync();
            m.Should().BeOfType<PublicationStateChangedMessage<string>>()
                .Which.Output!.Value.Value.Should().Be("");
            await cReader.AssertCannotReadAsync();

            sp.SetValue("1");
            m = await cReader.AssertReadAsync();
            m.Should().BeOfType<PublicationStateChangedMessage<string>>()
                .Which.IsConsistent.Should().BeFalse();
            var pm = (PublicationMessage) m;
            pm.PublisherId.Should().Be(Publisher.Id);
            pm.PublicationId.Should().Be(p1!.Id);
            await cReader.AssertCannotReadAsync();

            sp.SetValue("12");
            // No auto-update after invalidation
            await cReader.AssertCannotReadAsync();

            (await Publisher.SubscribeAsync(cp.Channel1, p1!, true)).Should().BeTrue();
            m = await cReader.AssertReadAsync();
            m.Should().BeOfType<PublicationStateChangedMessage<string>>()
                .Which.IsConsistent.Should().BeTrue();
            m.Should().BeOfType<PublicationStateChangedMessage<string>>()
                .Which.Output!.Value.Value.Should().Be("12");
            await cReader.AssertCannotReadAsync();

            await p1.DisposeAsync();
            m = await cReader.AssertReadAsync();
            m.Should().BeOfType<PublicationAbsentsMessage>();
        }
    }
}
