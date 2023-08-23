using Stl.Fusion.Authentication;

namespace Stl.Fusion.Tests.Authentication;

public class AuthCommandSerializationTest(ITestOutputHelper @out) : TestBase(@out)
{
    [Fact]
    public void AuthCommandSerialization()
    {
        var user = new User("b", "bob").WithIdentity("g:1");
        var session = new Session("validSessionId");

        new AuthBackend_SignIn(session, user).PassThroughAllSerializers().User.Name.Should().Be(user.Name);
        new Auth_SignOut(session, true).PassThroughAllSerializers().Session.Should().Be(session);
        new Auth_EditUser(session, "X").PassThroughAllSerializers().Session.Should().Be(session);
        new AuthBackend_SetupSession(session, "a", "b").PassThroughAllSerializers().Session.Should().Be(session);
        var sso = new Auth_SetSessionOptions(session, ImmutableOptionSet.Empty.Set(true), 1);
        sso.Options.GetOrDefault<bool>().Should().BeTrue();
        sso.ExpectedVersion.Should().Be(1);
    }
}
