using System.Diagnostics;
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
    public class PublisherTest : FusionTestBase
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

            Debug.WriteLine("a1");
            await Publisher.SubscribeAsync(cp.Channel1, p1, true);
            Debug.WriteLine("a2");
            var m = await cReader.AssertReadAsync();
            m.Should().BeOfType<PublicationStateMessage<string>>()
                .Which.Output!.Value.Value.Should().Be("");
            Debug.WriteLine("a3");
            await cReader.AssertCannotReadAsync();

            Debug.WriteLine("b1");
            sp.SetValue("1");
            Debug.WriteLine("b2");
            m = await cReader.AssertReadAsync();
            Debug.WriteLine("b3");
            m.Should().BeOfType<PublicationStateMessage<string>>()
                .Which.IsConsistent.Should().BeFalse();
            Debug.WriteLine("b4");
            var pm = (PublicationMessage) m;
            pm.PublisherId.Should().Be(Publisher.Id);
            pm.PublicationId.Should().Be(p1.Id);
            Debug.WriteLine("b5");
            await cReader.AssertCannotReadAsync();

            Debug.WriteLine("c1");
            sp.SetValue("12");
            // No auto-update after invalidation
            Debug.WriteLine("c2");
            await cReader.AssertCannotReadAsync();

            Debug.WriteLine("d1");
            await Publisher.SubscribeAsync(cp.Channel1, p1, true);
            Debug.WriteLine("d2");
            m = await cReader.AssertReadAsync();
            Debug.WriteLine("d3");
            m.Should().BeOfType<PublicationStateMessage<string>>()
                .Which.IsConsistent.Should().BeTrue();
            m.Should().BeOfType<PublicationStateMessage<string>>()
                .Which.Output!.Value.Value.Should().Be("12");
            Debug.WriteLine("d4");
            await cReader.AssertCannotReadAsync();

            Debug.WriteLine("e1");
            await p1.DisposeAsync();
            Debug.WriteLine("e2");
            await cReader.AssertCannotReadAsync();

            Debug.WriteLine("f1");
            await Publisher.SubscribeAsync(cp.Channel1, p1, true);
            Debug.WriteLine("f2");
            m = await cReader.AssertReadAsync();
            Debug.WriteLine("f3");
            m.Should().BeOfType<PublicationAbsentsMessage>();
        }
    }
}
