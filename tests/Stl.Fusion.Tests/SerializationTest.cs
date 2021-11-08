using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Commands;
using Stl.Fusion.Tests.Services;

namespace Stl.Fusion.Tests;

public class SerializationTest : TestBase
{
    public SerializationTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public void SessionSerialization()
    {
        default(Session).AssertPassesThroughAllSerializers(Out);
        Session.Null.AssertPassesThroughAllSerializers(Out);
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
    public void AuthCommandSerialization()
    {
        var user = new User("b", "bob").WithIdentity("g:1");
        var session = new Session("validSessionId");

        new SignInCommand(session, user).PassThroughAllSerializers().User.Name.Should().Be(user.Name);
        new SignOutCommand(session, true).PassThroughAllSerializers().Session.Should().Be(session);
        new EditUserCommand(session, "X").PassThroughAllSerializers().Session.Should().Be(session);
        new SetupSessionCommand(session, "a", "b").PassThroughAllSerializers().Session.Should().Be(session);
    }

    [Fact]
    public void ScreenshotSerialization()
    {
        var s = new Screenshot {
            Width = 10,
            Height = 20,
            CapturedAt = Moment.EpochStart,
            Image = new Base64Encoded(new byte[] { 1, 2, 3 })
        };
        s.AssertPassesThroughAllSerializers();
    }
}
