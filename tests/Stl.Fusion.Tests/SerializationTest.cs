using Stl.Fusion.Authentication;
using Stl.Fusion.Tests.Model;
using Stl.Fusion.Tests.Services;
using Stl.Generators;
using User = Stl.Fusion.Authentication.User;

namespace Stl.Fusion.Tests;

public class SerializationTest : TestBase
{
    public SerializationTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public void SessionSerialization()
    {
        default(Session).AssertPassesThroughAllSerializers(Out);
        new Session("0123456789-0123456789").AssertPassesThroughAllSerializers(Out);
    }

    [Fact]
    public void UserSerialization()
    {
        void AssertEquals(User some, User expected) {
            some.Id.Should().Be(expected.Id);
            some.Name.Should().Be(expected.Name);
            some.Version.Should().Be(expected.Version);
            some.Claims.Should().BeEquivalentTo(expected.Claims);
            some.Identities.Should().BeEquivalentTo(expected.Identities);
        }

        var user = new User("b", "bob");
        AssertEquals(user.PassThroughAllSerializers(Out), user);

        user = new User("b", "bob") { Version = 3 }
            .WithClaim("email1", "bob1@bob.bom")
            .WithClaim("email2", "bob2@bob.bom")
            .WithIdentity("google/1", "s")
            .WithIdentity("google/2", "q");
        AssertEquals(user.PassThroughAllSerializers(Out), user);
    }

    [Fact]
    public void TestCommandSerialization()
    {
        var c = new TestCommand<HasStringId>("1", new("2")).PassThroughAllSerializers();
        c.Id.Should().Be("1");
        c.Value!.Id.Should().Be("2");
    }

    [Fact]
    public void ScreenshotSerialization()
    {
        var s = new Screenshot {
            Width = 10,
            Height = 20,
            CapturedAt = SystemClock.Now,
            Image = new byte[] { 1, 2, 3 },
        };
        var t = s.PassThroughAllSerializers();
        t.Width.Should().Be(s.Width);
        t.Height.Should().Be(s.Height);
        t.CapturedAt.Should().Be(s.CapturedAt);
        t.Image.Should().Equal(s.Image);
    }

    [Fact]
    public void Base64EncodedSerialization()
    {
        var s = new Base64Encoded(new byte[] { 1, 2, 3 });
        s.AssertPassesThroughAllSerializers();
    }

    [Fact]
    public void SessionAuthInfoSerialization()
    {
        var si = new SessionAuthInfo(new Session(RandomStringGenerator.Default.Next())) {
            UserId = RandomStringGenerator.Default.Next(),
            AuthenticatedIdentity = new UserIdentity("a", "b"),
            IsSignOutForced = true,
        };
        Test(si);

        void Test(SessionAuthInfo s) {
            var t = s.PassThroughAllSerializers();
            t.SessionHash.Should().Be(s.SessionHash);
            t.UserId.Should().Be(s.UserId);
            t.AuthenticatedIdentity.Should().Be(s.AuthenticatedIdentity);
            t.IsSignOutForced.Should().Be(s.IsSignOutForced);
        }
    }

    [Fact]
    public void SessionInfoSerialization()
    {
        var si = new SessionInfo(new Session(RandomStringGenerator.Default.Next())) {
            Version = 1,
            CreatedAt = SystemClock.Now,
            LastSeenAt = SystemClock.Now + TimeSpan.FromSeconds(1),
            UserId = RandomStringGenerator.Default.Next(),
            AuthenticatedIdentity = new UserIdentity("a", "b"),
            IPAddress = "1.1.1.1",
            UserAgent = "None",
            IsSignOutForced = true,
        };
        si.Options.Set((Symbol)"test");
        si.Options.Set(true);
        Test(si);

        void Test(SessionInfo s) {
            var t = s.PassThroughAllSerializers();
            t.SessionHash.Should().Be(s.SessionHash);
            t.Version.Should().Be(s.Version);
            t.CreatedAt.Should().Be(s.CreatedAt);
            t.LastSeenAt.Should().Be(s.LastSeenAt);
            t.IPAddress.Should().Be(s.IPAddress);
            t.UserAgent.Should().Be(s.UserAgent);
            t.AuthenticatedIdentity.Should().Be(s.AuthenticatedIdentity);
            t.IsSignOutForced.Should().Be(s.IsSignOutForced);
            AssertEqual(s.Options, t.Options);
        }
    }

    private static void AssertEqual(ImmutableOptionSet a, ImmutableOptionSet b)
    {
        b.Items.Count.Should().Be(a.Items.Count);
        foreach (var (key, item) in b.Items)
            item.Should().Be(a[key]);
    }
}
