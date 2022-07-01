using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Messages;
using Stl.Fusion.Tests.Services;

namespace Stl.Fusion.Tests;

public class PublisherTest : FusionTestBase
{
    public PublisherTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public async Task CommunicationTest()
    {
        await using var serving = await WebHost.Serve();
        var publisher = WebServices.GetRequiredService<IPublisher>();
        using var wss = WebServices.CreateScope();
        var sp = wss.ServiceProvider.GetRequiredService<ISimplestProvider>();

        var cp = CreateChannelPair("c1");
        publisher.ChannelHub.Attach(cp.Channel1).Should().BeTrue();
        var cReader = cp.Channel2.Reader;

        sp.SetValue("");

        var p1 = await publisher.Publish(_ => sp.GetValue());
        p1.Should().NotBeNull();

        Debug.WriteLine("a1");
        await publisher.Subscribe(cp.Channel1, p1, true);
        Debug.WriteLine("a2");
        var m = await cReader.AssertRead();
        m.Should().BeOfType<PublicationStateReply<string, string>>()
            .Which.Output!.Value.Value.Should().Be("");
        Debug.WriteLine("a3");
        await cReader.AssertCannotRead();

        Debug.WriteLine("b1");
        sp.SetValue("1");
        Debug.WriteLine("b2");
        m = await cReader.AssertRead();
        Debug.WriteLine("b3");
        m.Should().BeOfType<PublicationStateReply<string>>()
            .Which.IsConsistent.Should().BeFalse();
        Debug.WriteLine("b4");
        var pm = (PublicationReply) m;
        pm.PublisherId.Should().Be(publisher.Id);
        pm.PublicationId.Should().Be(p1.Id);
        Debug.WriteLine("b5");
        await cReader.AssertCannotRead();

        Debug.WriteLine("c1");
        sp.SetValue("12");
        // No auto-update after invalidation
        Debug.WriteLine("c2");
        await cReader.AssertCannotRead();

        Debug.WriteLine("d1");
        await publisher.Subscribe(cp.Channel1, p1, true);
        Debug.WriteLine("d2");
        m = await cReader.AssertRead();
        Debug.WriteLine("d3");
        m.Should().BeOfType<PublicationStateReply<string, string>>()
            .Which.IsConsistent.Should().BeTrue();
        m.Should().BeOfType<PublicationStateReply<string, string>>()
            .Which.Output!.Value.Value.Should().Be("12");
        Debug.WriteLine("d4");
        await cReader.AssertCannotRead();

        Debug.WriteLine("e1");
        p1.Dispose();
        Debug.WriteLine("e2");
        await cReader.AssertCannotRead();

        Debug.WriteLine("f1");
        await publisher.Subscribe(cp.Channel1, p1, true);
        Debug.WriteLine("f2");
        m = await cReader.AssertRead();
        Debug.WriteLine("f3");
        m.Should().BeOfType<PublicationAbsentsReply>();
    }
}
